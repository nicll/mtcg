using NpgsqlTypes;
using System;

namespace MtcgServer.Databases.Postgres
{
    internal class CardRequirement
    {
        [PgName("req_type")]
        public CardRequirementType ReqType;

        [PgName("req_value")]
        public int ReqValue;
    }
}
