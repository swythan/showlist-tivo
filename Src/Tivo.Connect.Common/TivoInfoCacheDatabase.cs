using System.Collections.Generic;
using Tivo.Connect.Entities;
using Wintellect.Sterling.Database;

namespace Tivo.Connect
{
    public class TivoInfoCacheDatabase : BaseDatabaseInstance
    {
        public override string Name
        {
            get
            {
                return "TivoInfoCacheDatabase";
            }
        }

        protected override List<ITableDefinition> RegisterTables()
        {
            return new List<ITableDefinition>
            {
                this.CreateTableDefinition<IndividualShow, long>(x => x.ObjectId),
                this.CreateTableDefinition<Container, long>(x => x.ObjectId)
            };
        }
    }
}
