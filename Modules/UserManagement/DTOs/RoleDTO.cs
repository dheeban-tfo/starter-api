﻿namespace starterapi.DTOs
{
    public class RoleDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<RoleModulePermissionDTO> RoleModulePermissions { get; set; }
    }

  public class RoleModulePermissionDTO
{
    public int RoleId { get; set; }
    public int ModuleId { get; set; }
    public int ActionId { get; set; }
   // public string ModuleName { get; set; }
}

    public class UserRoleDTO
    {
        public Guid UserId { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }
    }
}