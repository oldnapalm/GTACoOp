using ProtoBuf;

namespace GTAServer.ProtocolMessages {
    [ProtoContract]
    public class Quaternion {
        [ProtoMember(1)]
        public float X { get; set; }
        [ProtoMember(2)]
        public float Y { get; set; }
        [ProtoMember(3)]
        public float Z { get; set; }
        [ProtoMember(4)]
        public float W { get; set; }

        public override string ToString()
        {
            return $"<X: {X}, Y: {Y}, Z: {Z}, W: {W}>";
        }
    }
}