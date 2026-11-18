using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using TrangSach.Shared.DTO;
using TrangSach.Shared.Security;

namespace TrangSach.BUS
{
    public class AdminRecordsService
    {
        private readonly BookStoreDao dao;
        private readonly IPasswordHashingService passwordHasher;

        // Danh sách được load vào RAM
        private List<ManagerCategoryDto> _categories = new();
        private List<ManagerAuthorDto> _authors = new();
        private List<ManagerPublisherDto> _publishers = new();
        private List<ManagerCustomerDto> _customers = new();
        private List<ManagerEmployeeDto> _employees = new();
        private List<StaffRoleOptionDto> _roles = new();

        public AdminRecordsService(BookStoreDao dao, IPasswordHashingService passwordHasher)
        {
            this.dao = dao;
            this.passwordHasher = passwordHasher;
        }

        // ---- READ ----
        public IReadOnlyList<ManagerCategoryDto> GetCategories() => _categories;
        public IReadOnlyList<ManagerAuthorDto> GetAuthors() => _authors;
        public IReadOnlyList<ManagerPublisherDto> GetPublishers() => _publishers;
        public IReadOnlyList<ManagerCustomerDto> GetCustomers() => _customers;
        public IReadOnlyList<ManagerEmployeeDto> GetEmployees() => _employees;
        public IReadOnlyList<StaffRoleOptionDto> GetEmployeeRoles() => _roles;

        // ----------------------------------------------------------------
        // Synchronous Methods (Phong cách thiết kế 3 lớp truyền thống)
        // ----------------------------------------------------------------
        public IReadOnlyList<ManagerCategoryDto> GetAllCategoriesList()
        {
            if (_categories.Count == 0) _categories = dao.Catalog.GetAllCategoryDetails();
            return _categories;
        }

        public IReadOnlyList<ManagerAuthorDto> GetAllAuthorsList()
        {
            if (_authors.Count == 0) _authors = dao.Catalog.GetAllAuthorDetails();
            return _authors;
        }

        public IReadOnlyList<ManagerPublisherDto> GetAllPublishersList()
        {
            if (_publishers.Count == 0) _publishers = dao.Catalog.GetAllPublisherDetails();
            return _publishers;
        }

        public IReadOnlyList<ManagerCustomerDto> GetAllCustomersList()
        {
            if (_customers.Count == 0) _customers = dao.Users.GetAllCustomers();
            return _customers;
        }

        public IReadOnlyList<ManagerEmployeeDto> GetAllEmployeesList()
        {
            if (_employees.Count == 0) _employees = dao.Users.GetAllEmployees();
            return _employees;
        }

        public IReadOnlyList<StaffRoleOptionDto> GetAllEmployeeRolesList()
        {
            if (_roles.Count == 0)
            {
                var roleNames = dao.Users.GetAllRoles().Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
                _roles = StaffPermissions.EmployeeRoles.Where(x => roleNames.Contains(x.Name)).Select(x => new StaffRoleOptionDto(x.Name, x.Label)).ToList();
            }
            return _roles;
        }

        public OperationResult<string> CreateCategory(SaveCategoryDto command)
        {
            if (string.IsNullOrWhiteSpace(command.Id) || command.Id.Trim().Length > 20 || string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length < 2 || command.Name.Trim().Length > 120)
                return OperationResult<string>.Invalid("Mã hoặc tên loại sách không hợp lệ.");

            var id = command.Id.Trim();
            if (_categories.Any(x => x.Id == id) || dao.Catalog.CategoryExists(id))
                return OperationResult<string>.Conflict("Mã loại sách đã tồn tại.");

            dao.Catalog.InsertCategory(id, command.Name.Trim());
            _categories.Add(new ManagerCategoryDto(id, command.Name.Trim()));
            return OperationResult<string>.Created(id);
        }

        public OperationResult<Unit> UpdateCategory(string id, SaveCategoryDto command)
        {
            if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length < 2 || command.Name.Trim().Length > 120)
                return OperationResult<Unit>.Invalid("Tên loại sách không hợp lệ.");

            if (_categories.Count == 0) GetAllCategoriesList();
            var cached = _categories.FirstOrDefault(x => x.Id == id);
            if (cached == null) return OperationResult<Unit>.NotFound();

            dao.Catalog.UpdateCategory(id, command.Name.Trim());
            cached.Name = command.Name.Trim();
            return OperationResult<Unit>.Success(new Unit());
        }

        public OperationResult<Unit> DeleteCategory(string id)
        {
            if (_categories.Count == 0) GetAllCategoriesList();
            var cached = _categories.FirstOrDefault(x => x.Id == id);
            if (cached == null) return OperationResult<Unit>.NotFound();

            if (dao.Catalog.CategoryHasBooks(id))
                return OperationResult<Unit>.Conflict("Không thể xóa loại sách đang chứa các đầu sách.");

            dao.Catalog.DeleteCategory(id);
            _categories.RemoveAll(x => x.Id == id);
            return OperationResult<Unit>.Success(new Unit());
        }

        public OperationResult<int> CreateAuthor(SaveAuthorDto command)
        {
            if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length < 2 || command.Name.Trim().Length > 150)
                return OperationResult<int>.Invalid("Tên tác giả không hợp lệ.");

            var newId = dao.Catalog.InsertAuthor(command.Name.Trim(), command.Biography?.Trim());
            _authors.Add(new ManagerAuthorDto(newId, command.Name.Trim(), command.Biography?.Trim()));
            return OperationResult<int>.Created(newId);
        }

        public OperationResult<Unit> UpdateAuthor(int id, SaveAuthorDto command)
        {
            if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length < 2 || command.Name.Trim().Length > 150)
                return OperationResult<Unit>.Invalid("Tên tác giả không hợp lệ.");

            if (_authors.Count == 0) GetAllAuthorsList();
            var cached = _authors.FirstOrDefault(x => x.Id == id);
            if (cached == null) return OperationResult<Unit>.NotFound();

            dao.Catalog.UpdateAuthor(id, command.Name.Trim(), command.Biography?.Trim());
            cached.Name = command.Name.Trim();
            cached.Biography = command.Biography?.Trim();
            return OperationResult<Unit>.Success(new Unit());
        }

        public OperationResult<Unit> DeleteAuthor(int id)
        {
            if (_authors.Count == 0) GetAllAuthorsList();
            var cached = _authors.FirstOrDefault(x => x.Id == id);
            if (cached == null) return OperationResult<Unit>.NotFound();

            if (dao.Catalog.AuthorHasBooks(id))
                return OperationResult<Unit>.Conflict("Không thể xóa tác giả đang gắn với đầu sách.");

            dao.Catalog.DeleteAuthor(id);
            _authors.RemoveAll(x => x.Id == id);
            return OperationResult<Unit>.Success(new Unit());
        }

        public OperationResult<int> CreatePublisher(SavePublisherDto command)
        {
            if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length < 2 || command.Name.Trim().Length > 150)
                return OperationResult<int>.Invalid("Tên nhà xuất bản không hợp lệ.");

            var newId = dao.Catalog.InsertPublisher(command.Name.Trim(), command.Address?.Trim());
            _publishers.Add(new ManagerPublisherDto(newId, command.Name.Trim(), command.Address?.Trim()));
            return OperationResult<int>.Created(newId);
        }

        public OperationResult<Unit> UpdatePublisher(int id, SavePublisherDto command)
        {
            if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length < 2 || command.Name.Trim().Length > 150)
                return OperationResult<Unit>.Invalid("Tên nhà xuất bản không hợp lệ.");

            if (_publishers.Count == 0) GetAllPublishersList();
            var cached = _publishers.FirstOrDefault(x => x.Id == id);
            if (cached == null) return OperationResult<Unit>.NotFound();

            dao.Catalog.UpdatePublisher(id, command.Name.Trim(), command.Address?.Trim());
            cached.Name = command.Name.Trim();
            cached.Address = command.Address?.Trim();
            return OperationResult<Unit>.Success(new Unit());
        }

        public OperationResult<Unit> DeletePublisher(int id)
        {
            if (_publishers.Count == 0) GetAllPublishersList();
            var cached = _publishers.FirstOrDefault(x => x.Id == id);
            if (cached == null) return OperationResult<Unit>.NotFound();

            if (dao.Catalog.PublisherHasBooks(id))
                return OperationResult<Unit>.Conflict("Không thể xóa nhà xuất bản đang gắn với đầu sách.");

            dao.Catalog.DeletePublisher(id);
            _publishers.RemoveAll(x => x.Id == id);
            return OperationResult<Unit>.Success(new Unit());
        }

        public OperationResult<string> CreateCustomer(SaveCustomerDto command)
        {
            var validation = ValidateCustomer(command);
            if (validation != null) return OperationResult<string>.Invalid(validation);

            int? accountId = null;
            if (HasAccount(command.UserName, command.Email, command.Password))
            {
                var accountError = ValidateNewAccount(command.UserName, command.Email, command.Password);
                if (accountError != null) return OperationResult<string>.Conflict(accountError);

                var roleId = dao.Users.GetRoleIdByName("Customer");
                if (!roleId.HasValue) return OperationResult<string>.Invalid("Không tìm thấy vai trò Khách hàng.");

                var hash = passwordHasher.Hash(command.UserName!.Trim(), command.Password!);
                accountId = dao.Users.CreateAccount(command.UserName.Trim(), command.Email!.Trim(), hash, roleId.Value, command.AccountStatus ?? "Active");
            }

            var id = !string.IsNullOrWhiteSpace(command.Id) ? command.Id.Trim() : $"KH{Convert.ToHexString(RandomNumberGenerator.GetBytes(5))}";
            dao.Users.InsertCustomer(id, command.FullName.Trim(), command.PhoneNumber?.Trim(), command.Address?.Trim(), accountId);

            _customers.Add(new ManagerCustomerDto(
                id, command.FullName.Trim(), command.PhoneNumber?.Trim() ?? "", command.Address?.Trim() ?? "",
                accountId, command.UserName?.Trim() ?? "", command.Email?.Trim() ?? "", command.AccountStatus ?? "Active"));

            return OperationResult<string>.Created(id);
        }

        public OperationResult<Unit> UpdateCustomer(string id, SaveCustomerDto command)
        {
            var validation = ValidateCustomer(command);
            if (validation != null) return OperationResult<Unit>.Invalid(validation);

            if (_customers.Count == 0) GetAllCustomersList();
            var cached = _customers.FirstOrDefault(x => x.Id == id);
            if (cached == null) return OperationResult<Unit>.NotFound();

            int? accountId = cached.AccountId;
            if (accountId.HasValue)
            {
                if (dao.Users.ExistsUserNameOrEmail(command.UserName ?? cached.UserName, command.Email ?? cached.Email, accountId.Value))
                    return OperationResult<Unit>.Conflict("Tên đăng nhập hoặc email đã được sử dụng.");

                string? newHash = !string.IsNullOrWhiteSpace(command.Password)
                    ? passwordHasher.Hash(command.UserName ?? cached.UserName, command.Password)
                    : null;

                dao.Users.UpdateAccount(accountId.Value, command.UserName ?? cached.UserName, command.Email ?? cached.Email, newHash, command.AccountStatus ?? cached.AccountStatus, null);
            }
            else if (HasAccount(command.UserName, command.Email, command.Password))
            {
                var accountError = ValidateNewAccount(command.UserName, command.Email, command.Password);
                if (accountError != null) return OperationResult<Unit>.Conflict(accountError);

                var roleId = dao.Users.GetRoleIdByName("Customer");
                var hash = passwordHasher.Hash(command.UserName!.Trim(), command.Password!);
                accountId = dao.Users.CreateAccount(command.UserName.Trim(), command.Email!.Trim(), hash, roleId ?? 2, command.AccountStatus ?? "Active");
            }

            dao.Users.UpdateCustomer(id, command.FullName.Trim(), command.PhoneNumber?.Trim(), command.Address?.Trim(), accountId);

            cached.FullName = command.FullName.Trim();
            cached.PhoneNumber = command.PhoneNumber?.Trim() ?? "";
            cached.Address = command.Address?.Trim() ?? "";
            cached.AccountId = accountId;
            if (command.UserName != null) cached.UserName = command.UserName.Trim();
            if (command.Email != null) cached.Email = command.Email.Trim();
            if (command.AccountStatus != null) cached.AccountStatus = command.AccountStatus;

            return OperationResult<Unit>.Success(new Unit());
        }

        public OperationResult<Unit> DeleteCustomer(string id)
        {
            if (_customers.Count == 0) GetAllCustomersList();
            var cached = _customers.FirstOrDefault(x => x.Id == id);
            if (cached == null) return OperationResult<Unit>.NotFound();

            if (dao.Users.CustomerHasOrders(id))
                return OperationResult<Unit>.Conflict("Khách hàng đã có giao dịch mua sách; không thể xóa hồ sơ.");

            dao.Users.DeleteCustomer(id, cached.AccountId);
            _customers.RemoveAll(x => x.Id == id);
            return OperationResult<Unit>.Success(new Unit());
        }

        public OperationResult<string> CreateEmployee(SaveEmployeeDto command)
        {
            var validation = ValidateEmployee(command);
            if (validation != null) return OperationResult<string>.Invalid(validation);

            int? accountId = null;
            if (HasAccount(command.UserName, command.Email, command.Password))
            {
                var accountError = ValidateNewAccount(command.UserName, command.Email, command.Password);
                if (accountError != null) return OperationResult<string>.Conflict(accountError);

                var roleId = dao.Users.GetRoleIdByName(command.RoleName);
                if (!roleId.HasValue) return OperationResult<string>.Invalid("Vai trò nhân viên không hợp lệ.");

                var hash = passwordHasher.Hash(command.UserName!.Trim(), command.Password!);
                accountId = dao.Users.CreateAccount(command.UserName.Trim(), command.Email!.Trim(), hash, roleId.Value, command.AccountLoginStatus ?? "Active");
            }

            var id = !string.IsNullOrWhiteSpace(command.Id) ? command.Id.Trim() : $"NV{Convert.ToHexString(RandomNumberGenerator.GetBytes(5))}";
            dao.Users.InsertEmployee(id, command.LastName.Trim(), command.FirstName.Trim(), command.BirthDate, command.AvatarPath, command.Address?.Trim(), command.AccountStatus, command.BaseSalary, accountId);

            _employees.Add(new ManagerEmployeeDto(
                id, command.LastName.Trim(), command.FirstName.Trim(), command.BirthDate, command.AvatarPath,
                command.Address?.Trim() ?? "", command.AccountStatus, command.BaseSalary, accountId,
                command.UserName?.Trim() ?? "", command.Email?.Trim() ?? "", command.AccountLoginStatus ?? "Active", command.RoleName));

            return OperationResult<string>.Created(id);
        }

        public OperationResult<Unit> UpdateEmployee(string id, SaveEmployeeDto command, int? currentAccountId)
        {
            var validation = ValidateEmployee(command);
            if (validation != null) return OperationResult<Unit>.Invalid(validation);

            if (_employees.Count == 0) GetAllEmployeesList();
            var cached = _employees.FirstOrDefault(x => x.Id == id);
            if (cached == null) return OperationResult<Unit>.NotFound();

            int? accountId = cached.AccountId;
            if (accountId.HasValue)
            {
                if (dao.Users.ExistsUserNameOrEmail(command.UserName ?? cached.UserName, command.Email ?? cached.Email, accountId.Value))
                    return OperationResult<Unit>.Conflict("Tên đăng nhập hoặc email đã được sử dụng.");

                var roleId = dao.Users.GetRoleIdByName(command.RoleName);
                string? newHash = !string.IsNullOrWhiteSpace(command.Password)
                    ? passwordHasher.Hash(command.UserName ?? cached.UserName, command.Password)
                    : null;

                dao.Users.UpdateAccount(accountId.Value, command.UserName ?? cached.UserName, command.Email ?? cached.Email, newHash, command.AccountLoginStatus ?? cached.AccountStatus, roleId);
            }
            else if (HasAccount(command.UserName, command.Email, command.Password))
            {
                var accountError = ValidateNewAccount(command.UserName, command.Email, command.Password);
                if (accountError != null) return OperationResult<Unit>.Conflict(accountError);

                var roleId = dao.Users.GetRoleIdByName(command.RoleName);
                var hash = passwordHasher.Hash(command.UserName!.Trim(), command.Password!);
                accountId = dao.Users.CreateAccount(command.UserName.Trim(), command.Email!.Trim(), hash, roleId ?? 1, command.AccountLoginStatus ?? "Active");
            }

            dao.Users.UpdateEmployee(id, command.LastName.Trim(), command.FirstName.Trim(), command.BirthDate, command.AvatarPath, command.Address?.Trim(), command.AccountStatus, command.BaseSalary, accountId);

            cached.LastName = command.LastName.Trim();
            cached.FirstName = command.FirstName.Trim();
            cached.BirthDate = command.BirthDate;
            cached.AvatarPath = command.AvatarPath;
            cached.Address = command.Address?.Trim() ?? "";
            cached.AccountStatus = command.AccountStatus;
            cached.BaseSalary = command.BaseSalary;
            cached.AccountId = accountId;
            if (command.UserName != null) cached.UserName = command.UserName.Trim();
            if (command.Email != null) cached.Email = command.Email.Trim();
            if (command.AccountLoginStatus != null) cached.AccountStatus = command.AccountLoginStatus;
            cached.RoleName = command.RoleName;

            return OperationResult<Unit>.Success(new Unit());
        }

        public OperationResult<Unit> DeleteEmployee(string id, int? currentAccountId)
        {
            if (_employees.Count == 0) GetAllEmployeesList();
            var cached = _employees.FirstOrDefault(x => x.Id == id);
            if (cached == null) return OperationResult<Unit>.NotFound();

            if (currentAccountId.HasValue && cached.AccountId == currentAccountId.Value)
                return OperationResult<Unit>.Conflict("Không thể xóa tài khoản nhân viên đang đăng nhập.");

            if (dao.Users.EmployeeHasTransactions(id))
                return OperationResult<Unit>.Conflict("Nhân viên đã được ghi nhận trong nghiệp vụ; hãy ngừng hoạt động để giữ lịch sử.");

            dao.Users.DeleteEmployee(id, cached.AccountId);
            _employees.RemoveAll(x => x.Id == id);
            return OperationResult<Unit>.Success(new Unit());
        }

        private string? ValidateNewAccount(string? userName, string? email, string? password)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return "Để tạo tài khoản cần nhập tên đăng nhập, email và mật khẩu.";
            if (dao.Users.ExistsUserNameOrEmail(userName.Trim(), email.Trim(), null))
                return "Tên đăng nhập hoặc email đã được sử dụng.";
            return password.Length < 8 ? "Mật khẩu mới cần ít nhất 8 ký tự." : null;
        }

                private static bool HasAccount(string? userName, string? email, string? password)
            => !string.IsNullOrWhiteSpace(userName) || !string.IsNullOrWhiteSpace(email) || !string.IsNullOrWhiteSpace(password);

        private static string? ValidateCustomer(SaveCustomerDto x)
        {
            if ((x.Id != null && x.Id.Length > 20) || string.IsNullOrWhiteSpace(x.FullName) || x.FullName.Trim().Length < 2 || x.FullName.Trim().Length > 180
                || (x.PhoneNumber != null && x.PhoneNumber.Length > 30) || (!string.IsNullOrWhiteSpace(x.PhoneNumber) && !new PhoneAttribute().IsValid(x.PhoneNumber))
                || (x.Address != null && x.Address.Length > 500) || (x.UserName != null && x.UserName.Length > 100) || (x.Email != null && x.Email.Length > 254)
                || (x.Password != null && x.Password.Length > 100) || (!string.IsNullOrWhiteSpace(x.Email) && !new EmailAddressAttribute().IsValid(x.Email))
                || (x.AccountStatus != null && x.AccountStatus.Length > 30))
                return "Thông tin khách hàng hoặc tài khoản không hợp lệ.";
            return null;
        }

        private static string? ValidateEmployee(SaveEmployeeDto x)
        {
            if ((x.Id != null && x.Id.Length > 20) || string.IsNullOrWhiteSpace(x.LastName) || x.LastName.Trim().Length > 100
                || string.IsNullOrWhiteSpace(x.FirstName) || x.FirstName.Trim().Length < 1 || x.FirstName.Trim().Length > 60
                || (x.AvatarPath != null && x.AvatarPath.Length > 500) || (x.Address != null && x.Address.Length > 300)
                || string.IsNullOrWhiteSpace(x.AccountStatus) || x.AccountStatus.Length > 30
                || x.BaseSalary < 0 || x.BaseSalary > 999999999
                || (x.UserName != null && x.UserName.Length > 100) || (x.Email != null && x.Email.Length > 254)
                || (x.Password != null && x.Password.Length > 100) || (!string.IsNullOrWhiteSpace(x.Email) && !new EmailAddressAttribute().IsValid(x.Email))
                || (x.AccountLoginStatus != null && x.AccountLoginStatus.Length > 30) || string.IsNullOrWhiteSpace(x.RoleName) || x.RoleName.Length > 60)
                return "Thông tin nhân viên hoặc tài khoản không hợp lệ.";
            return null;
        }
    }
}
