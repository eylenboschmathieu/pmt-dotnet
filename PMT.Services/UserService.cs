using PMT.Data.Entities;
using PMT.Data.Repositories;

namespace PMT.Services;

public class SimpleRole {
    public int Id { get; set; }
    public string Name { get; set; } = null!;
}

public class UserDataDTO {
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string? Name { get; set; } = null!;
    public bool Active { get; set; }
    public string? CreatedBy { get; set; } = null!;
    public List<SimpleRole> Roles { get; set; } = [];
}

public class UpdateUserDTO {
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool Active { get; set; }
    public int[] Roles { get; set; } = [];
}

public class UserSelectDTO {
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Icon { get; set; } = null!;
}

public class UserDTO {
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public bool IsActive { get; set; }
    public string CreatedBy { get; set; } = null!;
    public List<Role> Roles { get; set; } = [];
}

public class UserService(IUserRepository _userRepo, IRoleRepository _roleRepo) {
    public async Task<User?> Create(User user) {
        user.Email = user.Email.ToLower();
        return await _userRepo.AddAsync(user);
    }

    public async Task<User?> FindById(int userId) {
        return await _userRepo.FindByIdAsync(userId);
    }

    public async Task<IEnumerable<User>> FindAll() {
        return await _userRepo.FindAllAsync();
    }

    public async Task<IEnumerable<User>> FindSelect() {
        return await _userRepo.FindSelect();
    }

    public async Task<IEnumerable<User>> FindAllActive() {
        return await _userRepo.FindAllActive();
    }

    public async Task<User?> FindByGoogleId(string googleId) {
        return await _userRepo.FindByGoogleId(googleId);
    }

    public async Task<User?> FindByEmail(string email) {
        return await _userRepo.FindByEmail(email);
    }

    public async Task<bool> Update(UpdateUserDTO dto) {
        User? user = await _userRepo.FindWithRolesById(dto.Id);

        if (user is null)
            return false;

        user.Active = dto.Active;
        user.Name = dto.Name;
        user.Roles = (await _roleRepo.FindByIds(dto.Roles)).ToList();

        return (await _userRepo.UpdateAsync(user)) is not null;
    }

    public async Task<User?> Update(User user) {
        if (user is null)
            return null;
        return await _userRepo.UpdateAsync(user);
    }

    public async Task<bool> Delete(int userId) {
        return await _userRepo.DeleteAsync(userId);
    }

    public async Task<bool> Delete(User user) {
        return await _userRepo.DeleteAsync(user.Id);
    }

    public async Task<bool> SetActive(int userId, bool active) {
        return await _userRepo.SetActive(userId, active);
    }

    public async Task<UserDataDTO?> GetUserData(int userId) {
        User? user = await _userRepo.FindUserData(userId);
        if (user is null)
            return null;
 
         return new UserDataDTO {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Active = user.Active,
            CreatedBy = user.CreatedBy?.Name ?? "Default",
            Roles = user.Roles.Select(e => new SimpleRole { Id = e.Id, Name = e.Name } ).ToList()
        };
    }
}

