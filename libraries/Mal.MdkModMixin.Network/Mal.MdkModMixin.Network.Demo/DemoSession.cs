using Mal.MdkModMixin.Network;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Utils;

namespace Mal.MdkModMixin.Network.Demo
{
    /// <summary>A greeting: whatever text one machine sends another.</summary>
    public struct Greeting
    {
        /// <summary>The greeting's words.</summary>
        public string Text;

        /// <summary>The bytes a greeting writes.</summary>
        public static int SizeOf(ref Greeting msg)
        {
            return ByteData.SizeOfString(msg.Text);
        }

        /// <summary>Writes a greeting.</summary>
        public static int Write(ref Greeting msg, byte[] data, ref int offset)
        {
            var start = offset;
            ByteData.WriteString(msg.Text, data, ref offset);
            return offset - start;
        }

        /// <summary>Reads a greeting.</summary>
        public static int Read(byte[] data, ref int offset, out Greeting msg)
        {
            var start = offset;
            msg = new Greeting();
            ByteData.TryReadString(data, ref offset, out msg.Text);
            return offset - start;
        }
    }

    /// <summary>A mod that greets the server as each player joins, and is greeted back.</summary>
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class DemoSession : MySessionComponentBase
    {
        NetworkChannel _channel;

        /// <summary>Builds the mod's channel and greets the server from a client.</summary>
        public override void BeforeStart()
        {
            _channel = NetworkChannel.Create("Mal.MdkModMixin.Network.Demo")
                .UsingModel<Greeting>(Greeting.SizeOf, Greeting.Read, Greeting.Write)
                .Build();
            _channel.ListenFor<Greeting>(OnGreeting);
            if (!NetworkChannel.Network.IsServer)
                _channel.SendToServer(new Greeting { Text = "Hello" }, true);
        }

        void OnGreeting(ushort channelId, Greeting msg, ulong senderId, bool fromServer)
        {
            MyLog.Default.WriteLine(string.Format("Network demo: {0} says {1}", senderId, msg.Text));
            if (NetworkChannel.Network.IsServer && !fromServer)
                _channel.Send(new Greeting { Text = "Welcome" }, senderId, true);
        }

        /// <summary>Closes the channel.</summary>
        protected override void UnloadData()
        {
            if (_channel != null)
                _channel.Dispose();
            _channel = null;
        }
    }
}
