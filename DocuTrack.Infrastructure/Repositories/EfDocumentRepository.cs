using DocuTrack.Core.Enums;
using DocuTrack.Core.Models;
using DocuTrack.Core.Repositories;
using DocuTrack.Core.Requests;
using DocuTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocuTrack.Infrastructure.Repositories
{
    public sealed class EfDocumentRepository : IDocumentRepository
    {
        private readonly DocuTrackDbContext _context;
        public EfDocumentRepository(DocuTrackDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Document> AddAsync(Document document, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(document);
            await _context.Documents.AddAsync(document, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return document;
        }

        public async Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        }

        public async Task<Document?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Documents.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        }

        public async Task<long> GetNextDocumentNumberAsync(CancellationToken cancellationToken = default)
        {
            var connection = _context.Database.GetDbConnection();

            bool shouldCloseConnection = connection.State != System.Data.ConnectionState.Open;

            if (shouldCloseConnection)
            {
                await connection.OpenAsync(cancellationToken);
            }
            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT NEXT VALUE FOR dbo.DocumentNumberSequence";

                object? result = await command.ExecuteScalarAsync(cancellationToken);

                if (result is null || result == DBNull.Value)
                {
                    throw new InvalidOperationException("Failed to retrieve the next document number from the database.");
                }
                return Convert.ToInt64(result);
            }
            finally
            {
                if (shouldCloseConnection)
                {
                    await connection.CloseAsync();
                }
            }
        }

        public async Task<Document> UpdateAsync(Document document, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(document);
            await _context.SaveChangesAsync(cancellationToken);
            return document;
        }

        public async Task DeleteAsync(Document document, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(document);
            _context.Documents.Remove(document);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<PagedResult<Document>> SearchAsync(DocumentQuery documentQuery, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(documentQuery);
            IQueryable<Document> query = _context.Documents.AsNoTracking();

            // Search
            if (!string.IsNullOrWhiteSpace(documentQuery.Search))
            {
                string search = documentQuery.Search.Trim();

                query = query.Where(d =>
                    d.DocumentNumber.Contains(search) ||
                    d.Title.Contains(search) ||
                    (d.Description != null && d.Description.Contains(search)) ||
                    d.Owner.Contains(search));
            }

            // Status filter
            if (documentQuery.Status.HasValue)
            {
                query = query.Where(d => d.Status == documentQuery.Status.Value);
            }

            // Department filter
            if (documentQuery.Department.HasValue)
            {
                query = query.Where(d => d.Department == documentQuery.Department.Value);
            }

            // Owner filter
            if (!string.IsNullOrWhiteSpace(documentQuery.Owner))
            {
                string owner = documentQuery.Owner.Trim();
                query = query.Where(d => d.Owner.Contains(owner));
            }

            // Created date range
            if (documentQuery.CreatedFrom.HasValue)
            {
                query = query.Where(d => d.CreatedAt >= documentQuery.CreatedFrom.Value);
            }

            if (documentQuery.CreatedTo.HasValue)
            {
                query = query.Where(d => d.CreatedAt <= documentQuery.CreatedTo.Value);
            }

            // Total records before pagination
            int totalCount = await query.CountAsync(cancellationToken);
            
            // Sorting
            query = ApplySorting(documentQuery, query);

            // Pagination
            List<Document> documents = await query
                .Skip((documentQuery.PageNumber - 1) * documentQuery.PageSize)
                .Take(documentQuery.PageSize)
                .ToListAsync(cancellationToken);

            int totalPages = (int)Math.Ceiling((double)totalCount / documentQuery.PageSize);

            return new PagedResult<Document>
            {
                Items = documents,
                PageNumber = documentQuery.PageNumber,
                PageSize = documentQuery.PageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasPreviousPage = documentQuery.PageNumber > 1,
                HasNextPage = documentQuery.PageNumber < totalPages
            };
        }

        private static IQueryable<Document> ApplySorting(DocumentQuery documentQuery, IQueryable<Document> query)
        {
            // Sorting
            IOrderedQueryable<Document> orderedQuery = (documentQuery.SortBy, documentQuery.SortDirection) switch
            {
                (DocumentSortField.DocumentNumber, SortDirection.Ascending) => query.OrderBy(d => d.DocumentNumber),
                (DocumentSortField.DocumentNumber, SortDirection.Descending) => query.OrderByDescending(d => d.DocumentNumber),

                (DocumentSortField.Title, SortDirection.Ascending) => query.OrderBy(d => d.Title),
                (DocumentSortField.Title, SortDirection.Descending) => query.OrderByDescending(d => d.Title),

                (DocumentSortField.Owner, SortDirection.Ascending) => query.OrderBy(d => d.Owner),
                (DocumentSortField.Owner, SortDirection.Descending) => query.OrderByDescending(d => d.Owner),

                (DocumentSortField.Status, SortDirection.Ascending) => query.OrderBy(d => d.Status),
                (DocumentSortField.Status, SortDirection.Descending) => query.OrderByDescending(d => d.Status),

                (DocumentSortField.LastUpdatedAt, SortDirection.Ascending) => query.OrderBy(d => d.LastUpdatedAt),
                (DocumentSortField.LastUpdatedAt, SortDirection.Descending) => query.OrderByDescending(d => d.LastUpdatedAt),

                (DocumentSortField.CreatedAt, SortDirection.Ascending) => query.OrderBy(d => d.CreatedAt),
                _ => query.OrderByDescending(d => d.CreatedAt), // Default sorting
            };
            return orderedQuery.ThenBy(d => d.Id);
        }
    }
}
