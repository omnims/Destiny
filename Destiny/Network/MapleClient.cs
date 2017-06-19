﻿using Destiny.Core.Network;
using System;
using System.Net.Sockets;
using Destiny.Maple;
using Destiny.Core.IO;
using Destiny.Maple.Characters;
using Destiny.Network;

namespace Destiny
{
    public sealed class MapleClient : Session
    {
        private PacketProcessor m_processor;
        private Func<MapleClient, bool> m_deathAction;

        public Account Account { get; set; }
        public Character Character { get; set; }

        public byte Channel { get; set; }

        public MapleClient(Socket socket, PacketProcessor processor, Func<MapleClient, bool> deathAction)
            : base(socket)
        {
            m_processor = processor;
            m_deathAction = deathAction;
        }

        protected override void Terminate()
        {
            if (this.Character != null)
            {
                this.Character.Save();

                this.Character.Map.Characters.Remove(this.Character);
            }

            m_deathAction(this);
        }

        protected override void Dispatch(byte[] buffer)
        {
            using (InPacket iPacket = new InPacket(buffer))
            {
                PacketHandler handler = m_processor[iPacket.OperationCode];

                if (handler != null)
                {
                    try
                    {
                        handler(this, iPacket);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                }
                else
                {
                    Log.Warn("[{0}] Unhandled packet from {1}: {2}", m_processor.Label, this.Host, iPacket.ToString());
                }
            }
        }

        public void Migrate(bool valid, short port)
        {
            using (OutPacket oPacket = new OutPacket(SendOps.MigrateCommand))
            {
                oPacket
                    .WriteBool(valid)
                    .WriteBytes(new byte[4] { 127, 0, 0, 1 })
                    .WriteShort(port);

                this.Send(oPacket);
            }
        }
    }
}
