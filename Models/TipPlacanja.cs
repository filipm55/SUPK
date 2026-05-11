using NpgsqlTypes;
namespace SUPK.Models;

public enum TipPlacanja
{
    [PgName("GOTOVINA")]
    Gotovina,

    [PgName("KARTICA")]
    Kartica
}