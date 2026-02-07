using Microsoft.EntityFrameworkCore;

using PMT.Data;
using PMT.Data.Entities;
using PMT.Data.Repositories;

namespace PMT.Tests.Repository;

[TestClass]
public sealed class RoleTests {
    private RoleRepository _repo = null!;

    [TestInitialize]
    public async Task Init() {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"InMemoryDatabase{Guid.NewGuid()}")
            .Options;

        var context = new ApplicationDbContext(options);
        Role[] roles = [
            new Role { Id = 1, Name = "Admin", DelegationDepth = int.MaxValue },
            new Role { Id = 2, Name = "Manager", ParentId = 1, DelegationDepth = 1 },
            new Role { Id = 3, Name = "Paramedic", ParentId = 2, DelegationDepth = 0 },
            new Role { Id = 4, Name = "Doctor", ParentId = 2, DelegationDepth = 1 },
            new Role { Id = 5, Name = "Nurse", ParentId = 4, DelegationDepth = 0 }
        ];

        User[] users = [
            new User {
                Id = 1,
                Email = "john@gmail.com",
                Roles = [
                    roles[0]
                ]
            },
            new User {
                Id = 2,
                Email = "jane@gmail.com",
                Roles = [
                    roles[1], roles[2]
                ]
            }
        ];

        await context.Roles.AddRangeAsync(roles);
        await context.Users.AddRangeAsync(users);

        await context.SaveChangesAsync();
        _repo = new RoleRepository(context);
    }

    [TestMethod]
    public async Task GetAllRoles() {
        IEnumerable<Role> roles = await _repo.GetAllAsync();
        Assert.HasCount(5, roles);
    }

    [TestMethod]
    public async Task FindRolesById() {
        Role? admin = await _repo.GetAsync(1);
        Role? nurse = await _repo.GetAsync(5);
        Role? none = await _repo.GetAsync(123);

        Assert.AreEqual("Admin", admin!.Name);
        Assert.AreEqual("Nurse", nurse!.Name);
        Assert.IsNull(none);
    }

    [TestMethod]
    public async Task FindRolesByIds() {
        IEnumerable<int> ids = [ 2, 3 ];
        List<Role> roles = (await _repo.FindByIds(ids)).OrderBy(e => e.Id).ToList();

        Assert.HasCount(2, roles);
        Assert.AreEqual(2, roles[0].Id);
        Assert.AreEqual(3, roles[1].Id);
    }

    [TestMethod]
    public async Task FindRoleByName() {
        Role? para = await _repo.FindByName("Paramedic");
        Role? none = await _repo.FindByName("Foo");

        Assert.AreEqual(3, para!.Id);
        Assert.IsNull(none);
    }

    [TestMethod]
    public async Task FindRolesByParentId() {
        IEnumerable<Role> roles = await _repo.FindByParentId(2);
        
        Assert.HasCount(2, roles);
    }

    [TestMethod]
    public async Task FindRolesByParentIds() {
        IEnumerable<int> ids = [2, 4];
        List<Role> roles = (await _repo.FindByParentIds(ids)).OrderBy(e => e.Id).ToList();

        Assert.HasCount(3, roles);
        Assert.AreEqual(3, roles[0].Id);
        Assert.AreEqual(4, roles[1].Id);
        Assert.AreEqual(5, roles[2].Id);
    }

    [TestMethod]
    public async Task FindRoleByUser() {
        IEnumerable<Role> r1 = await _repo.FindByUser(new User { Id = 1 });
        IEnumerable<Role> r2 = await _repo.FindByUser(new User { Id = 2 });

        IEnumerable<Role> i1 = await _repo.FindByUser(1);
        IEnumerable<Role> i2 = await _repo.FindByUser(2);

        Assert.HasCount(1, r1);
        Assert.HasCount(2, r2);

        Assert.AreEqual("Admin", r1.First().Name);
        Assert.AreEqual("Manager", r2.First().Name);
        Assert.AreEqual("Paramedic", r2.Last().Name);
    }

    [TestMethod]
    public async Task CreateNewRole() {
        Role newRole = new() {
          Name = "TestRole",
          ParentId = 1,
          DelegationDepth = 0,  
        };
        Role role = await _repo.CreateAsync(newRole);
        Assert.AreEqual("TestRole", role.Name);
    }

    [TestMethod]
    public async Task UpdateRole() {
        Role? r = await _repo.GetAsync(1);
        Assert.IsNotNull(r);

        r.Name = "UpdateTest";
        Assert.IsTrue(await _repo.UpdateAsync(r));
    }

    [TestMethod]
    public async Task DeleteRole() {
        Assert.IsTrue(await _repo.DeleteAsync(5));
        Assert.IsNull(await _repo.GetAsync(5));
    }
}