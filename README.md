# NeMo.bil – offener Planungsvertrag

Offener Vertrag zwischen einer Mobilitätsplattform und einer austauschbaren Optimierung, mit
Referenz-Implementierung und ausführbarer Konformitätssuite. Entstanden im Verbundvorhaben
**NeMo.bil** (Arbeitspaket 4.3, Anschlusskonzepte) bei der reisewitz GmbH & Co. KG.

> *English summary:* This repository contains the open planning contract (`IPlanningEngine`) of the
> NeMo.bil research project, a reference implementation and an executable conformance test suite.
> Third parties can build their own — including proprietary — optimisation engine against the contract
> and verify it with the suite. The production optimisation is intentionally not part of this
> repository. Documentation is in German.

## Worum es geht

Eine Plattform für bedarfsgesteuerte Mobilität muss Fahrtwünsche in die Fahrpläne einer Fahrzeugflotte
einplanen. In NeMo.bil ist diese Aufgabe in zwei Bausteine getrennt:

- die **Anbindung** an die Plattform, die Fahrtwünsche und Flottenzustand entgegennimmt und
  Ergebnisse zurückmeldet, und
- die **Optimierung**, die entscheidet, welches Fahrzeug eine Fahrt wann übernimmt.

Beide wirken ausschließlich über einen neutralen Vertrag zusammen: die Schnittstelle `IPlanningEngine`
mit ihrem Datenmodell. Der Leitsatz lautet **„Was & Warum offen – Wie gekapselt“**. Der Vertrag
beschreibt vollständig, was eine Optimierung erhält, was sie liefern muss und welche Zusicherungen gelten.
Wie sie zu ihrem Ergebnis kommt, bleibt ihre Sache. So lassen sich unterschiedliche Verfahren gegen
dieselbe Schnittstelle entwickeln und vergleichen.

## Inhalt

| Pfad | Inhalt |
|------|--------|
| `src/Nemobil.Planning.Contracts` | Der Vertrag: `IPlanningEngine`, das neutrale Planungsmodell und die offenen Standard-Implementierungen `FleetSnapshot` und `PlanningItemList`. Keine Abhängigkeiten außer .NET. |
| `src/Nemobil.Planning.Reference` | Offene Referenz-Implementierung `ReferencePlanningEngine`: ein bewusst einfaches Einfügeverfahren mit austauschbarem Streckenmodell. |
| `src/Nemobil.Planning.Conformance` | Die ausführbare Konformitätssuite (xUnit) mit 14 synthetischen Prüffällen unter `Fixtures/`. |
| `src/Broker.Contracts` | Das NGSI-LD-Datenmodell der Anbindung: die Nachrichten, die Plattform und Planung über den FIWARE-Context-Broker austauschen (Fahrtwunsch, Fahrt, Vorschlag, Fahrzeug, Ladepunkt, Kopplungsorte und -routen), samt JSON-Konvertern für die NGSI-LD-Kurzformen. |
| `tests/Nemobil.Planning.Reference.Tests` | Die Referenz gegen die Suite, dazu Gegenproben, die zeigen, dass jede Prüfregel Verstöße erkennt. |
| `tests/Broker.Contracts.Tests` | Tests der JSON-Form des Datenmodells. |
| `.github/workflows/ci.yml` | Baut die Lösung und fährt alle Tests bei jedem Push und Pull Request. |
| `docs/` | Architektur- und Schnittstellendokumentation, Eingabe-/Wirkungs-Katalog und Konformitäts-Testkatalog. |

**Bewusst nicht enthalten** ist das in der Plattform eingesetzte Optimierungsverfahren. Es ist über den
Vertrag austauschbar angebunden und wird nicht veröffentlicht. Die Referenz-Implementierung ist kein
Abbild dieses Verfahrens.

## Schnellstart

Voraussetzung ist das [.NET SDK 10](https://dotnet.microsoft.com/download). Die Bibliotheken zielen auf
.NET 8 und lassen sich daher auch aus Anwendungen mit .NET 8 oder neuer verwenden.

```bash
dotnet build
dotnet test
```

Alle Pakete stammen von nuget.org; weitere Paketquellen sind nicht nötig.

## Eigene Optimierung gegen den Vertrag prüfen

1. `IPlanningEngine` implementieren. Alle Eingaben sind neutral lesbar: der Flottenzustand über
   `IFleetState.Vehicles`, die einzuplanenden Halte über `IPlanningItems.Items`.
2. In einem xUnit-Testprojekt (xUnit.net v2) das Projekt `src/Nemobil.Planning.Conformance` per
   Projektreferenz einbinden — ein NuGet-Paket gibt es nicht — und von der Suite ableiten:

   ```csharp
   public sealed class MeineEngineConformanceTests : PlanningEngineConformanceTests
   {
       protected override IPlanningEngine CreateEngine() => new MeineEngine();
   }
   ```

3. `dotnet test` ausführen. Jede Prüfgruppe läuft einmal je Prüffall; ein roter Test nennt die Verstöße
   im Klartext.

Details zu Prüfgruppen, Prüffällen und Toleranzen stehen im
[Konformitäts-Testkatalog](docs/Konformitaets-Testkatalog.md). Die Lesereihenfolge der Dokumentation
beginnt bei [docs/README.md](docs/README.md).

## Stand und Grenzen

Stand: September 2026.


- **Prototypisch.** Vertrag, Referenz und Suite sind im Rahmen eines Forschungsvorhabens entstanden und
  unter Laborbedingungen erprobt. Eine Validierung mit physischen Fahrzeugen hat nicht stattgefunden.
- **Von der Anbindungsschicht ist nur das Datenmodell enthalten** (`Broker.Contracts`), nicht ihre
  Logik. Ihr Verhalten ist im [Eingabe-/Wirkungs-Katalog](docs/Eingabe-Wirkungs-Katalog.md) beschrieben. Die Fälle, die nur die
  Anbindung prüfen kann (Buchung, Storno, Statuswechsel, Störung), sind im Testkatalog als solche
  gekennzeichnet.
- **Die Teil-Schnittstellen** für Konvoi- und Ladeplanung sind beschrieben, aber noch nicht Gegenstand
  der ausführbaren Suite.
- **Die Referenz-Implementierung** plant weder Konvois noch Ladehalte und optimiert nicht über mehrere
  Anfragen hinweg.
- **Die Prüffälle sind synthetisch** und frei erfunden. Sie stammen nicht aus einem Betrieb oder einem
  Datensatz Dritter.

## Förderung

Gefördert durch das Bundesministerium für Wirtschaft und Energie aufgrund eines Beschlusses des
Deutschen Bundestages, im Förderprogramm „Neue Fahrzeug- und Systemtechnologien“.

Teilvorhaben: Konzeption und Entwicklung von Systemkomponenten und Algorithmen zur Planung und Auslegung
eines schwarmartigen Mobilitätssystems.

Die inhaltliche Gesamtdarstellung enthält der Schlussbericht des Teilvorhabens, der bei der Technischen
Informationsbibliothek (TIB) Hannover hinterlegt wird.

Die Verantwortung für den Inhalt dieser Veröffentlichung liegt bei der reisewitz GmbH & Co. KG.

## Lizenz

[Apache License 2.0](LICENSE). Die Lizenz erlaubt ausdrücklich, eigene — auch proprietäre —
Optimierungsverfahren gegen den Vertrag zu entwickeln.

Verwendete Pakete Dritter: Mediator.Abstractions (MIT; nur die Nachrichten-Markerschnittstelle im
Datenmodell), xUnit.net (Apache-2.0) und Microsoft.NET.Test.Sdk (MIT) für Suite und Tests.
