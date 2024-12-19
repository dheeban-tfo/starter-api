using System;
using System.Collections.Generic;
using starterapi;

namespace StarterApi.Models.Communities
{
    public class Community : BaseEntity
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public ICollection<Block> Blocks { get; set; }
    }

    public class Block : BaseEntity
    {
        public string Name { get; set; }
        public int CommunityId { get; set; }
        public Community Community { get; set; }
        public ICollection<Floor> Floors { get; set; }
    }

    public class Floor : BaseEntity
    {
        public int FloorNumber { get; set; }
        public int BlockId { get; set; }
        public Block Block { get; set; }
        public ICollection<Unit> Units { get; set; }
    }

    public class Unit : BaseEntity
    {
        public string UnitNumber { get; set; }

        public UnitType Type { get; set; }
        
        public int FloorId { get; set; }
        public Floor Floor { get; set; }
        public ICollection<UnitOwnership> UnitOwnerships { get; set; }
        public ICollection<UnitResident> UnitResidents { get; set; }
    }

    public class UnitOwnership : BaseEntity
    {
        public int UnitId { get; set; }
        public Unit Unit { get; set; }
        
        public Guid UserId { get; set; }
        public User User { get; set; }
        public DateTime OwnershipStartDate { get; set; }
        public DateTime? OwnershipEndDate { get; set; }
        public decimal OwnershipPercentage { get; set; }
    }

    public class UnitResident : BaseEntity
    {
        public int UnitId { get; set; }
        public Unit Unit { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }
        public ResidentType ResidentType { get; set; }
        public DateTime MoveInDate { get; set; }
        public DateTime? MoveOutDate { get; set; }
    }

    public enum ResidentType
    {
        Owner,
        Tenant,
        FamilyMember
    }

    public enum UnitType
    {
        Studio,
        OneBHK,
        TwoBHK,
        ThreeBHK,
        Villa
    }
}
