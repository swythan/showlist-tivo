using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Formatters;

namespace TivoProxy
{
    public class SimpleEventTextFormatter : IEventTextFormatter
    {
        public void WriteEvent(EventEntry eventEntry, TextWriter writer)
        {
             writer.WriteLine(
                          "[{0}] {1} - {2}",
                          eventEntry.GetFormattedTimestamp(null),
                          eventEntry.Schema.EventName,
                          eventEntry.FormattedMessage);
        }
    }
}
