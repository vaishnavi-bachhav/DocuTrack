using DocuTrack.Domain.Documents;
using DocuTrack.Domain.Documents.Exceptions;
using FluentAssertions;

namespace DocuTrack.Domain.Tests.Documents
{
    public sealed class DocumentTests
    {
        private const string DocumentNumber = "DOC-000001";
        private const string Title = "Supplier Agreement";
        private const string Description = "Annual supplier agreement";
        private const string Owner = "Test Owner";

        private static readonly Guid CreatorUserId =
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        private static readonly Guid ModifierUserId =
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        private static readonly DateTimeOffset CreatedAt =
       new(
           2026,
           8,
           5,
           12,
           0,
           0,
           TimeSpan.Zero);


        #region Creation
        public void Create_ValidValues_CreatesDraftDocument()
        {
            // Act
            Document document = CreateDocument();

            // Assert
            document.Id.Should().NotBe(Guid.Empty);
            document.DocumentNumber.Should().Be(DocumentNumber);
            document.Title.Should().Be(Title);
            document.Description.Should().Be(Description);
            document.Type.Should().Be(DocumentType.Contract);
            document.Department.Should().Be(Department.Purchasing);
            document.Owner.Should().Be(Owner);

            document.Status.Should().Be(DocumentStatus.Draft);
            document.Version.Should().Be(1);

            document.CreatedByUserId.Should().Be(CreatorUserId);
            document.LastModifiedByUserId.Should().Be(CreatorUserId);

            document.CreatedAt.Should().Be(CreatedAt);
            document.LastUpdatedAt.Should().Be(CreatedAt);
        }

        [Fact]
        public void Create_ValuesWithWhitespace_TrimsTextValues()
        {
            // Act
            Document document = Document.Create(
                documentNumber: "  DOC-000001  ",
                title: "  Supplier Agreement  ",
                description: "  Annual supplier agreement  ",
                type: DocumentType.Contract,
                department: Department.Purchasing,
                owner: "  Test Owner  ",
                createdByUserId: CreatorUserId,
                createdAt: CreatedAt);

            // Assert
            document.DocumentNumber.Should().Be("DOC-000001");
            document.Title.Should().Be("Supplier Agreement");
            document.Description.Should().Be("Annual supplier agreement");
            document.Owner.Should().Be("Test Owner");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_MissingTitle_ThrowsArgumentException(
       string? invalidTitle)
        {
            // Act
            Action action = () => Document.Create(
                documentNumber: DocumentNumber,
                title: invalidTitle!,
                description: Description,
                type: DocumentType.Contract,
                department: Department.Purchasing,
                owner: Owner,
                createdByUserId: CreatorUserId,
                createdAt: CreatedAt);

            // Assert
            action.Should()
                .Throw<ArgumentException>()
                .WithMessage("*title*");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_MissingOwner_ThrowsArgumentException(
            string? invalidOwner)
        {
            // Act
            Action action = () => Document.Create(
                documentNumber: DocumentNumber,
                title: Title,
                description: Description,
                type: DocumentType.Contract,
                department: Department.Purchasing,
                owner: invalidOwner!,
                createdByUserId: CreatorUserId,
                createdAt: CreatedAt);

            // Assert
            action.Should()
                .Throw<ArgumentException>()
                .WithMessage("*owner*");
        }

        [Fact]
        public void Create_UnknownDocumentType_ThrowsArgumentException()
        {
            // Act
            Action action = () => Document.Create(
                documentNumber: DocumentNumber,
                title: Title,
                description: Description,
                type: DocumentType.Unknown,
                department: Department.Purchasing,
                owner: Owner,
                createdByUserId: CreatorUserId,
                createdAt: CreatedAt);

            // Assert
            action.Should()
                .Throw<ArgumentException>()
                .WithMessage("*type*");
        }

        [Fact]
        public void Create_UnknownDepartment_ThrowsArgumentException()
        {
            // Act
            Action action = () => Document.Create(
                documentNumber: DocumentNumber,
                title: Title,
                description: Description,
                type: DocumentType.Contract,
                department: Department.Unknown,
                owner: Owner,
                createdByUserId: CreatorUserId,
                createdAt: CreatedAt);

            // Assert
            action.Should()
                .Throw<ArgumentException>()
                .WithMessage("*department*");
        }

        [Fact]
        public void Create_WhitespaceDescription_StoresNull()
        {
            // Act
            Document document = Document.Create(
                documentNumber: DocumentNumber,
                title: Title,
                description: "   ",
                type: DocumentType.Contract,
                department: Department.Purchasing,
                owner: Owner,
                createdByUserId: CreatorUserId,
                createdAt: CreatedAt);

            // Assert
            document.Description.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_MissingDocumentNumber_ThrowsArgumentException(
    string? invalidDocumentNumber)
        {
            Action action = () => Document.Create(
                documentNumber: invalidDocumentNumber!,
                title: Title,
                description: Description,
                type: DocumentType.Contract,
                department: Department.Purchasing,
                owner: Owner,
                createdByUserId: CreatorUserId,
                createdAt: CreatedAt);

            action.Should()
                .Throw<ArgumentException>()
                .WithMessage("*document number*");
        }

        [Fact]
        public void Create_EmptyCreatorUserId_ThrowsArgumentException()
        {
            Action action = () => Document.Create(
                documentNumber: DocumentNumber,
                title: Title,
                description: Description,
                type: DocumentType.Contract,
                department: Department.Purchasing,
                owner: Owner,
                createdByUserId: Guid.Empty,
                createdAt: CreatedAt);

            action.Should()
                .Throw<ArgumentException>()
                .WithParameterName("createdByUserId");
        }

        #endregion

        #region Update details

        [Fact]
        public void UpdateDetails_ValidValues_UpdatesDocument()
        {
            // Arrange
            Document document = CreateDocument();

            DateTimeOffset modifiedAt =
                CreatedAt.AddHours(2);

            // Act
            document.UpdateDetails(
                title: "Updated Agreement",
                description: "Updated description",
                type: DocumentType.Invoice,
                department: Department.Legal,
                owner: "Updated Owner",
                modifiedByUserId: ModifierUserId,
                modifiedAt: modifiedAt);

            // Assert
            document.Title.Should().Be("Updated Agreement");
            document.Description.Should().Be("Updated description");
            document.Type.Should().Be(DocumentType.Invoice);
            document.Department.Should().Be(Department.Legal);
            document.Owner.Should().Be("Updated Owner");

            document.LastModifiedByUserId.Should().Be(ModifierUserId);
            document.LastUpdatedAt.Should().Be(modifiedAt);
            document.Version.Should().Be(2);

            document.CreatedByUserId.Should().Be(CreatorUserId);
            document.CreatedAt.Should().Be(CreatedAt);
        }

        [Fact]
        public void UpdateDetails_ValidValues_DoesNotChangeIdentityFields()
        {
            // Arrange
            Document document = CreateDocument();

            Guid originalId = document.Id;
            string originalDocumentNumber = document.DocumentNumber;
            Guid originalCreator = document.CreatedByUserId;
            DateTimeOffset originalCreatedAt = document.CreatedAt;

            // Act
            document.UpdateDetails(
                title: "Updated Agreement",
                description: null,
                type: DocumentType.Invoice,
                department: Department.Legal,
                owner: "Updated Owner",
                modifiedByUserId: ModifierUserId,
                modifiedAt: CreatedAt.AddHours(1));

            // Assert
            document.Id.Should().Be(originalId);
            document.DocumentNumber.Should().Be(originalDocumentNumber);
            document.CreatedByUserId.Should().Be(originalCreator);
            document.CreatedAt.Should().Be(originalCreatedAt);
        }

        [Fact]
        public void UpdateDetails_ValidValues_IncrementsVersionOnce()
        {
            // Arrange
            Document document = CreateDocument();

            // Act
            document.UpdateDetails(
                title: "Updated Agreement",
                description: null,
                type: DocumentType.Contract,
                department: Department.Legal,
                owner: "Updated Owner",
                modifiedByUserId: ModifierUserId,
                modifiedAt: CreatedAt.AddHours(1));

            // Assert
            document.Version.Should().Be(2);
        }

        [Fact]
        public void UpdateDetails_CalledTwice_IncrementsVersionForEachUpdate()
        {
            // Arrange
            Document document = CreateDocument();

            // Act
            document.UpdateDetails(
                title: "First Update",
                description: null,
                type: DocumentType.Contract,
                department: Department.Legal,
                owner: "Owner One",
                modifiedByUserId: ModifierUserId,
                modifiedAt: CreatedAt.AddHours(1));

            document.UpdateDetails(
                title: "Second Update",
                description: null,
                type: DocumentType.Invoice,
                department: Department.InformationTechnology,
                owner: "Owner Two",
                modifiedByUserId: ModifierUserId,
                modifiedAt: CreatedAt.AddHours(2));

            // Assert
            document.Version.Should().Be(3);
            document.Title.Should().Be("Second Update");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void UpdateDetails_MissingTitle_ThrowsAndDoesNotModifyDocument(
            string? invalidTitle)
        {
            // Arrange
            Document document = CreateDocument();

            string originalTitle = document.Title;
            int originalVersion = document.Version;
            DateTimeOffset originalUpdatedAt = document.LastUpdatedAt;

            // Act
            Action action = () => document.UpdateDetails(
                title: invalidTitle!,
                description: "Updated description",
                type: DocumentType.Invoice,
                department: Department.Legal,
                owner: "Updated Owner",
                modifiedByUserId: ModifierUserId,
                modifiedAt: CreatedAt.AddHours(1));

            // Assert
            action.Should().Throw<ArgumentException>();

            document.Title.Should().Be(originalTitle);
            document.Version.Should().Be(originalVersion);
            document.LastUpdatedAt.Should().Be(originalUpdatedAt);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void UpdateDetails_MissingOwner_ThrowsAndDoesNotModifyDocument(
            string? invalidOwner)
        {
            // Arrange
            Document document = CreateDocument();

            string originalOwner = document.Owner;
            int originalVersion = document.Version;

            // Act
            Action action = () => document.UpdateDetails(
                title: "Updated title",
                description: "Updated description",
                type: DocumentType.Invoice,
                department: Department.Legal,
                owner: invalidOwner!,
                modifiedByUserId: ModifierUserId,
                modifiedAt: CreatedAt.AddHours(1));

            // Assert
            action.Should().Throw<ArgumentException>();

            document.Owner.Should().Be(originalOwner);
            document.Version.Should().Be(originalVersion);
        }

        [Fact]
        public void UpdateDetails_WhitespaceDescription_StoresNull()
        {
            // Arrange
            Document document = CreateDocument();

            // Act
            document.UpdateDetails(
                title: "Updated title",
                description: "   ",
                type: DocumentType.Contract,
                department: Department.Legal,
                owner: "Updated Owner",
                modifiedByUserId: ModifierUserId,
                modifiedAt: CreatedAt.AddHours(1));

            // Assert
            document.Description.Should().BeNull();
        }

        #endregion

        #region Valid status transitions

        [Theory]
        [InlineData(DocumentStatus.Draft, DocumentStatus.Uploaded)]
        [InlineData(DocumentStatus.Uploaded, DocumentStatus.UnderReview)]
        [InlineData(
            DocumentStatus.UnderReview,
            DocumentStatus.PendingApproval)]
        [InlineData(
            DocumentStatus.PendingApproval,
            DocumentStatus.Approved)]
        [InlineData(
            DocumentStatus.PendingApproval,
            DocumentStatus.Rejected)]
        [InlineData(DocumentStatus.Rejected, DocumentStatus.Draft)]
        [InlineData(DocumentStatus.Approved, DocumentStatus.Archived)]
        public void ChangeStatus_ValidTransition_ChangesStatus(
            DocumentStatus currentStatus,
            DocumentStatus newStatus)
        {
            // Arrange
            Document document =
                CreateDocumentAtStatus(currentStatus);

            int originalVersion = document.Version;

            DateTimeOffset modifiedAt =
                CreatedAt.AddDays(1);

            // Act
            document.ChangeStatus(
                newStatus,
                ModifierUserId,
                modifiedAt);

            // Assert
            document.Status.Should().Be(newStatus);
            document.Version.Should().Be(originalVersion + 1);
            document.LastModifiedByUserId.Should().Be(ModifierUserId);
            document.LastUpdatedAt.Should().Be(modifiedAt);
        }

        [Fact]
        public void ChangeStatus_CompleteApprovalWorkflow_EndsAsArchived()
        {
            // Arrange
            Document document = CreateDocument();

            // Act
            document.ChangeStatus(
                DocumentStatus.Uploaded,
                ModifierUserId,
                CreatedAt.AddMinutes(1));

            document.ChangeStatus(
                DocumentStatus.UnderReview,
                ModifierUserId,
                CreatedAt.AddMinutes(2));

            document.ChangeStatus(
                DocumentStatus.PendingApproval,
                ModifierUserId,
                CreatedAt.AddMinutes(3));

            document.ChangeStatus(
                DocumentStatus.Approved,
                ModifierUserId,
                CreatedAt.AddMinutes(4));

            document.ChangeStatus(
                DocumentStatus.Archived,
                ModifierUserId,
                CreatedAt.AddMinutes(5));

            // Assert
            document.Status.Should().Be(DocumentStatus.Archived);
            document.Version.Should().Be(6);
        }

        [Fact]
        public void ChangeStatus_RejectionWorkflow_CanReturnToDraft()
        {
            // Arrange
            Document document = CreateDocument();

            document.ChangeStatus(
                DocumentStatus.Uploaded,
                ModifierUserId,
                CreatedAt.AddMinutes(1));

            document.ChangeStatus(
                DocumentStatus.UnderReview,
                ModifierUserId,
                CreatedAt.AddMinutes(2));

            document.ChangeStatus(
                DocumentStatus.PendingApproval,
                ModifierUserId,
                CreatedAt.AddMinutes(3));

            document.ChangeStatus(
                DocumentStatus.Rejected,
                ModifierUserId,
                CreatedAt.AddMinutes(4));

            // Act
            document.ChangeStatus(
                DocumentStatus.Draft,
                ModifierUserId,
                CreatedAt.AddMinutes(5));

            // Assert
            document.Status.Should().Be(DocumentStatus.Draft);
            document.Version.Should().Be(6);
        }

        #endregion

        #region Invalid status transitions

        [Theory]
        [InlineData(DocumentStatus.Draft, DocumentStatus.Approved)]
        [InlineData(DocumentStatus.Draft, DocumentStatus.Archived)]
        [InlineData(DocumentStatus.Draft, DocumentStatus.Rejected)]
        [InlineData(DocumentStatus.Uploaded, DocumentStatus.Approved)]
        [InlineData(DocumentStatus.Uploaded, DocumentStatus.Archived)]
        [InlineData(DocumentStatus.UnderReview, DocumentStatus.Approved)]
        [InlineData(DocumentStatus.Approved, DocumentStatus.Draft)]
        [InlineData(DocumentStatus.Archived, DocumentStatus.Draft)]
        [InlineData(DocumentStatus.Rejected, DocumentStatus.Approved)]
        public void ChangeStatus_InvalidTransition_ThrowsException(
            DocumentStatus currentStatus,
            DocumentStatus invalidNewStatus)
        {
            // Arrange
            Document document =
                CreateDocumentAtStatus(currentStatus);

            int originalVersion = document.Version;
            DateTimeOffset originalUpdatedAt = document.LastUpdatedAt;
            Guid? originalModifier = document.LastModifiedByUserId;

            // Act
            Action action = () => document.ChangeStatus(
                invalidNewStatus,
                ModifierUserId,
                CreatedAt.AddDays(1));

            // Assert
            action.Should()
                .Throw<InvalidDocumentStatusTransitionException>();

            document.Status.Should().Be(currentStatus);
            document.Version.Should().Be(originalVersion);
            document.LastUpdatedAt.Should().Be(originalUpdatedAt);
            document.LastModifiedByUserId.Should().Be(originalModifier);
        }

        [Theory]
        [InlineData(DocumentStatus.Draft)]
        [InlineData(DocumentStatus.Uploaded)]
        [InlineData(DocumentStatus.UnderReview)]
        [InlineData(DocumentStatus.PendingApproval)]
        [InlineData(DocumentStatus.Approved)]
        [InlineData(DocumentStatus.Rejected)]
        [InlineData(DocumentStatus.Archived)]
        public void ChangeStatus_ToSameStatus_ThrowsException(
            DocumentStatus currentStatus)
        {
            // Arrange
            Document document =
                CreateDocumentAtStatus(currentStatus);

            int originalVersion = document.Version;

            // Act
            Action action = () => document.ChangeStatus(
                currentStatus,
                ModifierUserId,
                CreatedAt.AddDays(1));

            // Assert
            action.Should()
                .Throw<InvalidDocumentStatusTransitionException>();

            document.Status.Should().Be(currentStatus);
            document.Version.Should().Be(originalVersion);
        }

        [Fact]
        public void ChangeStatus_ToUnknown_ThrowsException()
        {
            // Arrange
            Document document = CreateDocument();

            // Act
            Action action = () => document.ChangeStatus(
                DocumentStatus.Unknown,
                ModifierUserId,
                CreatedAt.AddDays(1));

            // Assert
            action.Should()
                .Throw<InvalidDocumentStatusTransitionException>();

            document.Status.Should().Be(DocumentStatus.Draft);
            document.Version.Should().Be(1);
        }

        #endregion

        #region Deletion rules

        [Theory]
        [InlineData(DocumentStatus.Draft)]
        [InlineData(DocumentStatus.Rejected)]
        public void EnsureCanDelete_AllowedStatus_DoesNotThrow(
            DocumentStatus status)
        {
            // Arrange
            Document document =
                CreateDocumentAtStatus(status);

            // Act
            Action action = document.EnsureCanDelete;

            // Assert
            action.Should().NotThrow();
        }

        [Theory]
        [InlineData(DocumentStatus.Uploaded)]
        [InlineData(DocumentStatus.UnderReview)]
        [InlineData(DocumentStatus.PendingApproval)]
        [InlineData(DocumentStatus.Approved)]
        [InlineData(DocumentStatus.Archived)]
        public void EnsureCanDelete_DisallowedStatus_ThrowsException(
            DocumentStatus status)
        {
            // Arrange
            Document document =
                CreateDocumentAtStatus(status);

            // Act
            Action action = document.EnsureCanDelete;

            // Assert
            DocumentDeletionNotAllowedException exception =
                action.Should()
                    .Throw<DocumentDeletionNotAllowedException>()
                    .Which;

            exception.Message.Should()
                .Contain(document.Id.ToString());

            exception.Message.Should()
                .Contain(status.ToString());
        }

        [Fact]
        public void EnsureCanDelete_DoesNotModifyDocument()
        {
            // Arrange
            Document document = CreateDocument();

            int originalVersion = document.Version;
            DocumentStatus originalStatus = document.Status;
            DateTimeOffset originalUpdatedAt = document.LastUpdatedAt;

            // Act
            document.EnsureCanDelete();

            // Assert
            document.Version.Should().Be(originalVersion);
            document.Status.Should().Be(originalStatus);
            document.LastUpdatedAt.Should().Be(originalUpdatedAt);
        }

        #endregion

        private static Document CreateDocument()
        {
            return Document.Create(
                documentNumber: DocumentNumber,
                title: Title,
                description: Description,
                type: DocumentType.Contract,
                department: Department.Purchasing,
                owner: Owner,
                createdByUserId: CreatorUserId,
                createdAt: CreatedAt);
        }

        private static Document CreateDocumentAtStatus(
            DocumentStatus targetStatus)
        {
            Document document = CreateDocument();

            switch (targetStatus)
            {
                case DocumentStatus.Draft:
                    return document;

                case DocumentStatus.Uploaded:
                    MoveToUploaded(document);
                    return document;

                case DocumentStatus.UnderReview:
                    MoveToUnderReview(document);
                    return document;

                case DocumentStatus.PendingApproval:
                    MoveToPendingApproval(document);
                    return document;

                case DocumentStatus.Approved:
                    MoveToApproved(document);
                    return document;

                case DocumentStatus.Rejected:
                    MoveToPendingApproval(document);

                    document.ChangeStatus(
                        DocumentStatus.Rejected,
                        ModifierUserId,
                        CreatedAt.AddMinutes(4));

                    return document;

                case DocumentStatus.Archived:
                    MoveToApproved(document);

                    document.ChangeStatus(
                        DocumentStatus.Archived,
                        ModifierUserId,
                        CreatedAt.AddMinutes(5));

                    return document;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(targetStatus),
                        targetStatus,
                        "Unsupported document status for test setup.");
            }
        }

        private static void MoveToUploaded(
            Document document)
        {
            document.ChangeStatus(
                DocumentStatus.Uploaded,
                ModifierUserId,
                CreatedAt.AddMinutes(1));
        }

        private static void MoveToUnderReview(
            Document document)
        {
            MoveToUploaded(document);

            document.ChangeStatus(
                DocumentStatus.UnderReview,
                ModifierUserId,
                CreatedAt.AddMinutes(2));
        }

        private static void MoveToPendingApproval(
            Document document)
        {
            MoveToUnderReview(document);

            document.ChangeStatus(
                DocumentStatus.PendingApproval,
                ModifierUserId,
                CreatedAt.AddMinutes(3));
        }

        private static void MoveToApproved(
            Document document)
        {
            MoveToPendingApproval(document);

            document.ChangeStatus(
                DocumentStatus.Approved,
                ModifierUserId,
                CreatedAt.AddMinutes(4));
        }
    }
}
