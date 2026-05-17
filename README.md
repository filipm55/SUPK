# SUPK

**Sustav za upravljanje stolovima i pozivanje konobara u kafiću** je ASP.NET Core MVC aplikacija za upravljanje radom caffe bara. Projekt pokriva pregled stanja, upravljanje računima, proizvodima, stolovima, konobarima i prometom, uz odvojene slojeve za prezentaciju, poslovnu logiku i pristup podacima. U trenutnom stanju omogućava samo ograničene funkcionalnosti.

## Glavne funkcionalnosti

- pregled početne stranice i stanja sustava
- upravljanje računima
- dodavanje i uređivanje stavki računa
- upravljanje proizvodima

## Preduvjeti

Za pokretanje su potrebni:

- `.NET 10 SDK`
- `PostgreSQL`
- Visual Studio ili `dotnet` CLI

## Instalacija

1. Klonirati repozitorij:

```bash
git clone https://github.com/filipm55/SUPK.git
cd SUPK
```

2. Provjeriti postavke baze u `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=SUPK;Username=postgres;Password=password"
}
```

3. Napraviti bazu podataka `SUPK` u PostgreSQL-u ako već ne postoji.

## Pokretanje aplikacije

Pokretanje iz naredbenog retka:

```bash
dotnet restore
dotnet build
dotnet run
```

Aplikacija će se otvoriti na lokalnoj adresi koju prikaže `dotnet run` ili Visual Studio.

## Pokretanje u Visual Studio

1. Otvoriti rješenje `SUPK.slnx`.
2. Postaviti projekt `SUPK` kao startup projekt.
3. Provjeriti konekcijski string u `appsettings.json`.
4. Pokrenuti aplikaciju tipkom `F5`.