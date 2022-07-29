#if !NETSTANDARD
using System;
using eCrestronExpanderType = Crestron.SimplSharpPro.AudioDistribution.eExpanderType;
#endif
namespace ICD.Connect.Audio.CrestronPro.Swamp
{
    
    public enum eExpanderType
    {
        None,
        SwampE8,
        SwampE4,
        Swe8
    }

#if !NETSTANDARD
    public static class ExpanderTypeExtensions
    {
        public static eCrestronExpanderType ToCrestron(this eExpanderType extends)
        {
            switch (extends)
            {
                case eExpanderType.None:
                    return eCrestronExpanderType.None;
                case eExpanderType.SwampE8:
                    return eCrestronExpanderType.SwampE8;
                case eExpanderType.SwampE4:
                    return eCrestronExpanderType.SwampE4;
                case eExpanderType.Swe8:
                    return eCrestronExpanderType.Swe8;
                default:
                    throw new ArgumentOutOfRangeException("extends");
            }
        }

        public static eExpanderType ToIcd(this eCrestronExpanderType extends)
        {
            switch (extends)
            {
                case eCrestronExpanderType.None:
                    return eExpanderType.None;
                case eCrestronExpanderType.SwampE8:
                    return eExpanderType.SwampE8;
                case eCrestronExpanderType.SwampE4:
                    return eExpanderType.SwampE4;
                case eCrestronExpanderType.Swe8:
                    return eExpanderType.Swe8;
                default:
                    throw new ArgumentOutOfRangeException("extends");
            }
        }
    }
#endif
}
