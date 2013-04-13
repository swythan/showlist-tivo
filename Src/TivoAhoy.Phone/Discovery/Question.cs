namespace TivoAhoy.Phone.Discovery
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public class Question : IServerResponse
    {
        private Question()
        {
            DomainName = new DomainName();

        }

        public Question(string domainName)
            : this()
        {
            if (!domainName.EndsWith("."))
            {
                if (domainName.EndsWith(".local"))
                    domainName += ".";
                else
                    domainName += ".local.";
            }
            else
            {
                if (!domainName.EndsWith("local."))
                    domainName += "local.";
            }
            DomainName.AddRange(domainName.Split('.'));
            Type = QType.ALL;
            Class = QClass.ALL;
        }

        public DomainName DomainName { get; private set; }

        public QType Type { get; set; }
        public QClass Class { get; set; }

        public bool CacheFlush { get; set; }

        public byte[] ToBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(DomainName.ToBytes());
            bytes.AddRange(BinaryHelper.ToBytes((ushort)Type));
            bytes.AddRange(BinaryHelper.ToBytes((ushort)Class));
            //bytes.AddRange(Message.ToBytes((ushort)(((ushort)(ushort.MaxValue >> 15 << 15)) + (ushort)Class)));
            return bytes.ToArray();
        }

        public static Question FromBytes(byte[] bytes, ref int index)
        {
            Question q = new Question();
            q.DomainName = DomainName.FromBytes(bytes, ref index);
            ushort s;
            BinaryHelper.FromBytes(bytes, index, out s);
            index += 2;
            q.Type = (QType)s;
            BinaryHelper.FromBytes(bytes, index, out s);
            q.Class = (QClass)s;
            index += 2;
/* openHAB, the open Home Automation Bus.
 * Copyright (C) 2010-${year}, openHAB.org <admin@openhab.org>
 * 
 * See the contributors.txt file in the distribution for a
 * full listing of individual contributors.
 * 
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as
 * published by the Free Software Foundation; either version 3 of the
 * License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, see <http://www.gnu.org/licenses>.
 * 
 * Additional permission under GNU GPL version 3 section 7
 * 
 * If you modify this Program, or any covered work, by linking or 
 * combining it with Eclipse (or a modified version of that library),
 * containing parts covered by the terms of the Eclipse Public License
 * (EPL), the licensors of this Program grant you additional permission
 * to convey the resulting work.
 */
 
            return q;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(DomainName);
            sb.AppendFormat(" QType : {0},", Type);
            sb.AppendFormat(" QClass : {0}", Class);
            return sb.ToString();
        }

        #region IResponse Members

        public void WriteTo(Stream stream)
        {
            DomainName.WriteTo(stream);
            BinaryHelper.Write(stream, (ushort)Type);
            BinaryHelper.Write(stream, (ushort)Class);
        }

        public byte[] GetBytes()
        {
            return BinaryHelper.GetBytes(this);
        }

        #endregion

        public static Question Get(BinaryReader stream)
        {
            Question q = new Question();
            q.DomainName = DomainName.Get(stream);
            q.Type = (QType)BinaryHelper.ReadUInt16(stream);
            ushort s = BinaryHelper.ReadUInt16(stream);
            q.Class = (QClass)((ushort)(s << 1) >> 1);
            q.CacheFlush = ((ushort)q.Class) != s;
            return q;
        }
    }
}
