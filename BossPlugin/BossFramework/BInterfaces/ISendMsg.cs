using Microsoft.Xna.Framework;

namespace BossFramework.BInterfaces
{
    public interface ISendMsg
    {
        public void SendSuccessMsg(object text);

        public void SendInfoMsg(object text);
        public void SendErrorMsg(object text);
        public void SendMsg(object text, Color color = default);
    }
}
