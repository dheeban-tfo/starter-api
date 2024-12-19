namespace StarterApi.DTOs
{
    public class BlockDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int CommunityId { get; set; }
        public string CommunityName { get; set; }
        public int NumberOfFloors { get; set; }
    }

    public class CreateBlockDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int CommunityId { get; set; }
        public int NumberOfFloors { get; set; }
    }

    public class UpdateBlockDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
} 