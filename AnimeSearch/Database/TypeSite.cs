namespace AnimeSearch.Database
{
    public class TypeSite
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public override bool Equals(object obj)
        {
            if(obj is not null and TypeSite other)
            {
                return Name == other.Name && Id == other.Id;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() & Name.GetHashCode();
        }
    }
}
