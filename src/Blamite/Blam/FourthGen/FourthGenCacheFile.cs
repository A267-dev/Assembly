using System.Collections.Generic;
using System.Linq;
using Blamite.Blam;
using Blamite.Blam.Localization;
using Blamite.Blam.Resources.Sounds;
using Blamite.Blam.Scripting;
using Blamite.Blam.Shaders;
using Blamite.Blam.FourthGen.Localization;
using Blamite.Blam.FourthGen.Resources;
using Blamite.Blam.FourthGen.Shaders;
using Blamite.Blam.FourthGen.Structures;
using Blamite.Blam.Util;
using Blamite.Serialization;
using Blamite.IO;
using Blamite.Util;

namespace Blamite.Blam.FourthGen
{
    /// <summary>
    ///     A Forth-Generation Blam (map/dat) cache file.
    /// </summary>
    public class FourthGenCacheFile
    {
        private readonly EngineDescription _buildInfo;
        private readonly FileSegmenter _segmenter;
        private IndexedFileNameSource _fileNames;
        private FourthGenHeader _header;
        private FourthGenLanguageGlobals _languageInfo;
        private FourthGenLanguagePackLoader _languageLoader;
        private FourthGenResourceMetaLoader _resourceMetaLoader;
        private IResourceManager _resources;
        private IndexedStringIDSource _stringIds;
        private FourthGenTagTable _tags;
        private FourthGenSimulationDefinitionTable _simulationDefinitions;
        private FourthGenPointerExpander _expander;
        private Endian _endianness;
        private EffectInterop _effects;
        private SoundResourceManager _soundGestalt;
        
        private bool _zoneOnly = false;

        public FourthGenCacheFile(IReader reader, EngineDescription buildInfo, string filePath)
        {
            FilePath = filePath;
            _endianness = reader.Endianness;
            _buildInfo = buildInfo;
            _segmenter = FileSegmenter(buildInfo.SegmentAlignment);
            _expander = FourthGenPointerExpander(buildInfo.ExpandMagic);
            Allocator = MetaAllocator(this, 0x10000);
            Load(reader);
        }

        public FourthGenHeader FullHeader
        {
            get { return _header; }
        }

        public void SaveChanges(Istream stream)
        {
            _tags.SaveChanges(stream);
            _stringIds.SaveChanges(stream)
            if (_simulationDefinitions !=null)
                _simulationDefinitions.SaveChanges(stream);
			if (_effects != null)
				_effects.SaveChanges(stream);
			int checksumOffset = WriteHeader(stream);
			WriteLanguageInfo(stream);

            if (checksumOffset != -1)
			{
				_header.Checksum = ICacheFileExtensions.GenerateChecksum(this, stream);
				stream.SeekTo(checksumOffset);
				stream.WriteUInt32(_header.Checksum);
			}
        }

        public string FilePath { get; private set; }

        public int HeaderSize
		{
			get { return _header.HeaderSize; }
		}

		public long FileSize
		{
			get { return _header.FileSize; }
		}

		public CacheFileType Type
		{
			get { return _header.Type; }
		}

		public EngineType Engine
		{
			get { return EngineType.ThirdGeneration; }
		}

		public string InternalName
		{
			get { return _header.InternalName; }
		}

		public string ScenarioName
		{
			get { return _header.ScenarioName; }
		}

		public string BuildString
		{
			get { return _header.BuildString; }
		}

		public int XDKVersion
		{
			get { return _header.XDKVersion; }
			set { _header.XDKVersion = value; }
		}

		public bool ZoneOnly
		{
			get { return _zoneOnly; }
		}

		public SegmentPointer IndexHeaderLocation
		{
			get { return _header.IndexHeaderLocation; }
			set { _header.IndexHeaderLocation = value; }
		}

		public Partition[] Partitions
		{
			get { return _header.Partitions; }
		}

		public FileSegment RawTable
		{
			get { return _header.RawTable; }
		}

		public FileSegmentGroup StringArea
		{
			get { return _header.StringArea; }
		}

		public FileNameSource FileNames
		{
			get { return _fileNames; }
		}

		public StringIDSource StringIDs
		{
			get { return _stringIds; }
		}

		public IList<ITagGroup> TagGroups
		{
			get { return _tags.Groups; }
		}

		public IResourceManager Resources
		{
			get { return _resources; }
		}

		public TagTable Tags
		{
			get { return _tags; }
		}

		public FileSegmentGroup MetaArea
		{
			get { return _header.MetaArea; }
		}

		public FileSegmentGroup LocaleArea
		{
			get { return (_languageInfo != null ? _languageInfo.LocaleArea : null); }
		}

		public ILanguagePackLoader Languages
		{
			get { return _languageLoader; }
		}

		public IResourceMetaLoader ResourceMetaLoader
		{
			get { return _resourceMetaLoader; }
		}

		public IEnumerable<FileSegment> Segments
		{
			get { return _segmenter.GetWrappers(); }
		}

		public FileSegment StringIDIndexTable
		{
			get { return _header.StringIDIndexTable; }
		}

		public FileSegment StringIDDataTable
		{
			get { return _header.StringIDData; }
		}

		public FileSegment FileNameIndexTable
		{
			get { return _header.FileNameIndexTable; }
		}

		public FileSegment FileNameDataTable
		{
			get { return _header.FileNameData; }
		}

		public MetaAllocator Allocator { get; private set; }

		public IScriptFile[] ScriptFiles { get; private set; }

		public IShaderStreamer ShaderStreamer { get; private set; }

		public ISimulationDefinitionTable SimulationDefinitions
		{
			get { return _simulationDefinitions; }
		}

		public IList<ITagInterop> TagInteropTable
		{
			get { return _tags.Interops; }
		}

		public IPointerExpander PointerExpander
		{
			get { return _expander; }
		}

		public Endian Endianness
		{
			get { return _endianness; }
		}

		public EffectInterop EffectInterops
		{
			get { return _effects; }
		}

		public SoundResourceManager SoundGestalt
		{
			get { return _soundGestalt; }
		}

		private void Load(IReader reader)
        {
            LoadHeader(reader);
			LoadFileNames(reader);
			var resolver = LoadStringIDNamespaces(reader);
			LoadStringIDs(reader, resolver);
			LoadTags(reader);
			LoadLanguageGlobals(reader);
			LoadScriptFiles();
			LoadResourceManager(reader);
			LoadSoundResourceManager(reader);
			LoadSimulationDefinitions(reader);
			LoadEffects(reader);
            ShaderStreamer = FourthGenShaderStreaner(this, _buildInfo);
        }

        private void LoadHeader(IReader reader)
		{
			reader.SeekTo(0);
			StructureValueCollection values = StructureReader.ReadStructure(reader, _buildInfo.Layouts.GetLayout("header"));
			_header = FourthGenHeader(values, _buildInfo, _segmenter, _expander);
    }
}