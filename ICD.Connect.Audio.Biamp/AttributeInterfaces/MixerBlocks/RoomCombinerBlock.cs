using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Codes;
using ICD.Connect.Audio.Biamp.TesiraTextProtocol.Parsing;

namespace ICD.Connect.Audio.Biamp.AttributeInterfaces.MixerBlocks
{
	public sealed class RoomCombinerBlock : AbstractMixerBlock
	{
	    private readonly Dictionary<int, RoomCombinerWall> m_Walls;
	    private readonly SafeCriticalSection m_WallsSection;
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="instanceTag"></param>
		public RoomCombinerBlock(BiampTesiraDevice device, string instanceTag)
			: base(device, instanceTag)
		{
            m_Walls = new Dictionary<int, RoomCombinerWall>();
            m_WallsSection = new SafeCriticalSection();

		}
        /// <summary>
        /// Lazy-loads the wall with the given id.
        /// </summary>
        /// <returns></returns>
        [PublicAPI]
        public RoomCombinerWall GetWall(int id)
        {
            m_WallsSection.Enter();
            try
            {
                if (!m_Walls.ContainsKey(id))
                    m_Walls.Add(id, new RoomCombinerWall(this, id));
                return m_Walls[id];
            }
            finally
            {
                m_WallsSection.Leave();
            }
        }
	}
}
