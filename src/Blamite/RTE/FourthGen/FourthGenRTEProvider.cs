using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Blamite.Blam;
using Blamite.IO;
using Blamite.Native;
using Blamite.Util;
using Blamite.Serialization;

namespace Blamite.RTE.Eldorado
{
	/// <summary>
	/// A real-time editing provider which connects to Halo Online.
	/// </summary>
	public class EldoradoRTEProvider : IRTEProvider
	{
		/// <summary>
		///     Constructs a new ThirdGenMCCRTEProvider.
		/// </summary>
		public EldoradoRTEProvider(EngineDescription engine) : base(engine)
		{ }

		/// <summary>
		///     Obtains a stream which can be used to read and write a cache file's meta in realtime.
		///     The stream will be set up such that offsets in the stream correspond to meta pointers in the cache file.
		/// </summary>
		/// <param name="cacheFile">The cache file to get a stream for.</param>
		/// <returns>The stream if it was opened successfully, or null otherwise.</returns>
		public override IStream GetMetaStream(ICacheFile cacheFile = null)
		{
			if (!CheckBuildInfo())
				return null;

			Process gameProcess = FindGameProcess();
			if (gameProcess == null)
				return null;

			PokingInformation info = RetrieveInformation(gameProcess);

			if (!info.HeaderPointer.HasValue && (!info.HeaderAddress.HasValue || !info.MagicAddress.HasValue))
				throw new NotImplementedException("Poking information is missing required values.");

			var gameMemory = new ProcessModuleMemoryStream(gameProcess, _buildInfo.GameModule);
			var mapInfo = new ThirdGenMapPointerReader(gameMemory, _buildInfo, info);

			long metaMagic = mapInfo.CurrentCacheAddress;

			if (gameMemory.BaseModule == null)
				return null;

			if (cacheFile != null && mapInfo.MapName != cacheFile.InternalName)
			{
				gameMemory.Close();
				return null;
			}
			
			if (metaMagic == 0)
				return null;

			var metaStream = new OffsetStream(gameMemory, metaMagic);
			return new EndianStream(metaStream, BitConverter.IsLittleEndian ? Endian.LittleEndian : Endian.BigEndian);
		}
	}

	/* public class MS25RTEProvider : EldoradoRTEProvider
	{
		public MS25RTEProvider(string exeName) : base(exeName)
		{
			MaxTagCountAddress = 0x40D44A8;
			TagIndexArrayPointerAddress = 0x40D449C;
			TagAddressArrayPointerAddress = 0x40D4498;
		}
	}

	public class ZBTRTEProvider : EldoradoRTEProvider
	{
		public ZBTRTEProvider(string exeName) : base(exeName)
		{
			MaxTagCountAddress = 0x42D68E8;
			TagIndexArrayPointerAddress = 0x42D68DC;
			TagAddressArrayPointerAddress = 0x42D68D8;
		}
	}

	public class ZBT70RTEProvider : EldoradoRTEProvider
	{
		public ZBT70RTEProvider(string exeName) : base(exeName)
		{
			MaxTagCountAddress = 0x3010B90;
			TagIndexArrayPointerAddress = 0x4503F6;
			TagAddressArrayPointerAddress = 0x450406;
		}
	}
	*/
}
