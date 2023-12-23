using System;

namespace RankMemberNamespace
{
    public class RankMember
    {
        public String Name { get; set; }
        public double Mass { get; set; }
        public RankMember()
        {

        }
        public RankMember(String name, double mass)
        {
            Name = name;
            Mass = mass;
        }
    }
}