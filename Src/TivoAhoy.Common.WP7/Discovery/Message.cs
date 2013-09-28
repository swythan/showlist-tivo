namespace TivoAhoy.Common.Discovery
{
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text;

    public class Message : BaseMessage<Message>
    {
        public Message(ushort id)
        {
            this.ID = id;
            this.Questions = new List<Question>();
            this.Answers = new List<Answer>();
            this.Authorities = new List<Answer>();
            this.Additionals = new List<Answer>();
        }

        public ushort ID { get; private set; }

        public IPEndPoint From { get; set; }

        public Qr QueryResponse { get; set; }

        public OpCode OpCode { get; set; }

        public bool AuthoritativeAnswer { get; set; }

        public bool Truncated { get; set; }

        public bool RecursionDesired { get; set; }

        public bool RecursionAvailable { get; set; }

        public ResponseCode ResponseCode { get; set; }

        public ushort QuestionEntries
        {
            get { return (ushort)this.Questions.Count; }
        }

        public ushort AnswerEntries
        {
            get { return (ushort)this.Answers.Count; }
        }

        public ushort AuthorityEntries
        {
            get { return (ushort)this.Authorities.Count; }
        }

        public ushort AdditionalEntries
        {
            get { return (ushort)this.Additionals.Count; }
        }

        public IList<Question> Questions { get; private set; }
        public IList<Answer> Answers { get; private set; }
        public IList<Answer> Authorities { get; private set; }
        public IList<Answer> Additionals { get; private set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(base.ToString());
            sb.AppendLine(string.Format("ID : {0}", this.ID));
            sb.AppendLine(string.Format("Query/Response : {0}", this.QueryResponse));
            sb.AppendLine(string.Format("OpCode : {0}", this.OpCode));
            sb.AppendLine(string.Format("Authoritative Answer : {0}", this.AuthoritativeAnswer));
            sb.AppendLine(string.Format("Truncated : {0}", this.Truncated));
            sb.AppendLine(string.Format("Recursion Desired : {0}", this.RecursionDesired));
            sb.AppendLine(string.Format("Recursion Available : {0}", this.RecursionDesired));
            sb.AppendLine(string.Format("Recursion ResponseCode : {0}", this.ResponseCode));

            sb.AppendLine(string.Format("Question Entries ({0}) :", this.QuestionEntries));

            foreach (Question q in this.Questions)
            {
                sb.AppendLine(q.ToString());
            }

            sb.AppendLine(string.Format("Answer Entries ({0}) :", this.AnswerEntries));

            foreach (Answer a in this.Answers)
            {
                sb.AppendLine(a.ToString());
            }

            return sb.ToString();
        }

        #region IResponse Members

        public override void WriteTo(Stream stream)
        {
            //ID
            BinaryHelper.Write(stream, this.ID);

            //Qr, Opcode, Aa, Tc, Rd
            byte b = 0;

            b += (byte)this.QueryResponse;
            b = (byte)(b << 4);
            b += (byte)this.OpCode;
            b = (byte)(b << 1);
            b += (this.AuthoritativeAnswer) ? (byte)1 : (byte)0;
            b = (byte)(b << 1);
            b += (this.Truncated) ? (byte)1 : (byte)0;
            b = (byte)(b << 1);
            b += (this.RecursionDesired) ? (byte)1 : (byte)0;
            stream.WriteByte(b);

            //Ra, Z, Rcode
            b = 0;
            b += (this.RecursionAvailable) ? (byte)1 : (byte)0;
            b = (byte)(b << 7);
            b += (byte)this.ResponseCode;
            stream.WriteByte(b);

            BinaryHelper.Write(stream, QuestionEntries);
            BinaryHelper.Write(stream, AnswerEntries);
            BinaryHelper.Write(stream, AuthorityEntries);
            BinaryHelper.Write(stream, AdditionalEntries);

            foreach (Question q in Questions)
            {
                q.WriteTo(stream);
            }

            foreach (Answer a in Answers)
            {
                a.WriteTo(stream);
            }

            foreach (Answer a in Authorities)
            {
                a.WriteTo(stream);
            }

            foreach (Answer a in Additionals)
            {
                a.WriteTo(stream);
            }
        }

        #endregion

        #region IRequest<Message> Members

        protected override Message GetMessage(Stream s)
        {
            return GetMessage(new BackReferenceBinaryReader(s, Encoding.BigEndianUnicode));
        }

        public static Message GetMessage(BinaryReader reader)
        {
            var backRefReader = reader as BackReferenceBinaryReader;
            if (backRefReader == null)
            {
                backRefReader = new BackReferenceBinaryReader(reader.BaseStream, Encoding.BigEndianUnicode);
            }

            return GetMessage(backRefReader);
        }

        private static Message GetMessage(BackReferenceBinaryReader reader)
        {
            ushort id = BinaryHelper.ReadUInt16(reader);
            Message m = new Message(id);

            byte b = reader.ReadByte();

            //Qr, Opcode, Aa, Tc, Rd
            m.RecursionDesired = (b % 2) == 1;
            b = (byte)(b >> 1);
            m.Truncated = (b % 2) == 1;
            b = (byte)(b >> 1);
            m.AuthoritativeAnswer = (b % 2) == 1;
            b = (byte)(b >> 1);
            int opCodeNumber = b % 16;
            m.OpCode = (OpCode)opCodeNumber;
            b = (byte)(b >> 4);
            m.QueryResponse = (Qr)b;

            //Ra, Z, Rcode
            b = reader.ReadByte();
            m.RecursionAvailable = b > 127;
            b = (byte)((b << 1) >> 1);
            m.ResponseCode = (ResponseCode)b;

            ushort questionEntryCount = BinaryHelper.ReadUInt16(reader);
            ushort answerEntryCount = BinaryHelper.ReadUInt16(reader);
            ushort authorityEntryCount = BinaryHelper.ReadUInt16(reader);
            ushort additionalEntryCount = BinaryHelper.ReadUInt16(reader);

            for (int i = 0; i < questionEntryCount; i++)
            {
                m.Questions.Add(Question.Get(reader));
            }

            for (int i = 0; i < answerEntryCount; i++)
            {
                m.Answers.Add(Answer.Get(reader));
            }

            for (int i = 0; i < authorityEntryCount; i++)
            {
                m.Authorities.Add(Answer.Get(reader));
            }

            for (int i = 0; i < additionalEntryCount; i++)
            {
                m.Additionals.Add(Answer.Get(reader));
            }

            return m;
        }
        #endregion
    }
}
