﻿using StarterApi.Models;

namespace starterapi;

public class CommunityDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
}

public class CommunityWithBlocksDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public List<BlockDto> Blocks { get; set; }
}

public class CommunityWithBlocksAndFloorsDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public List<BlockWithFloorsDto> Blocks { get; set; }
}

public class CommunityFullDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public List<BlockFullDto> Blocks { get; set; }
}

public class BlockDto
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class BlockWithFloorsDto : BlockDto
{
    public List<FloorDto> Floors { get; set; }
}

public class BlockFullDto : BlockDto
{
    public List<FloorFullDto> Floors { get; set; }
}

public class FloorDto
{
    public int Id { get; set; }
    public int FloorNumber { get; set; }
}

public class FloorFullDto : FloorDto
{
    public List<UnitDto> Units { get; set; }
}

public class UnitDto
{
    public int Id { get; set; }
    public string UnitNumber { get; set; }
    public UnitType Type { get; set; }
}