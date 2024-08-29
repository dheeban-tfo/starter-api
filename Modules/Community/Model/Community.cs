using System;
using System.Collections.Generic;

namespace StarterApi.Models
{
    public class Community
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<Block> Blocks { get; set; }
    }

    public class Block
    {
        public int Id { get; set; }
        public int CommunityId { get; set; }
        public string Name { get; set; }
        public List<Floor> Floors { get; set; }
    }

    public class Floor
    {
        public int Id { get; set; }
        public int BlockId { get; set; }
        public int FloorNumber { get; set; }
        public List<Unit> Units { get; set; }
    }

    public class Unit
    {
        public int Id { get; set; }
        public int FloorId { get; set; }
        public string UnitNumber { get; set; }
        public UnitType Type { get; set; }
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