using System.Runtime.InteropServices;

namespace TrProtocol.Models;

[StructLayout(LayoutKind.Sequential)]
public partial struct Buff
{
    public ushort BuffType { get; set; }
    public short BuffTime { get; set; }
}