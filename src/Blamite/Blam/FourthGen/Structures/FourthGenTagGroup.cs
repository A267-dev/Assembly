using Blamite.Serialization;

namespace Blamite.Blam.FourthGen.Structures
{
	public class FourthGenTagGroup : ITagGroup
	{
		public FourthGenTagGroup(StructureValueCollection values)
		{
			Load(values);
		}

		public int Magic { get; set; }
		public int ParentMagic { get; set; }
		public int GrandparentMagic { get; set; }
		public StringID Description { get; set; }

		private void Load(StructureValueCollection values)
		{
			Magic = (int) values.GetInteger("magic");
			ParentMagic = (int) values.GetInteger("parent magic");
			GrandparentMagic = (int) values.GetInteger("grandparent magic");
			Description = new StringID(values.GetIntegerOrDefault("stringid", 0));
		}
	}
}