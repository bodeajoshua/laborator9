<!--
  ȘABLON · JURNAL.md · Laborator Întâlnirea 4 
  Cum îl folosiți:
  1. Copiați fișierul în folderul rădăcina al solutiei voastre (lângă fișierul .slnx).
  2. Înlocuiți tot ce este între \\\[paranteze drepte] 
 

# Laboratorul Î4 · \\\Cirlig Bianca · nr. matricol \\\[121200054] · varianta \\\[A4 · B1]

|Parametru|Valoarea mea|
|-|-|
|A (ultima cifră a nr. matricol: \\\[4])|\\\[A4]|
|B (penultima cifră a nr. matricol: \\\[5])|\\\[B1]|
|V1 · cărți pe pagină|\\\[9]|
|V2 · sortarea implicită a catalogului|\\\[preț desc]|
|V3 · filtrul din S3|\\\[autor]|
|V4 · indicatorul din S2|\\\[preț mediu]|

## S1 · Comenzile

\* Protecția la overposting aleasă: \*\*\\\[Bind]\*\*
\* De ce am ales-o: \\\[specifica explicit proprietatile permise la trimiterea formularului. Pastram atat codul din controller curat si nu facem nici clase ViewModel suplimentare]

## S2 · Pagina unui autor și sortarea după autor

\* Indicatorul V4 (\\\[pret mediu]) este calculat cu interogarea:

```sql
\\\[SELECT AVG([b].[Price])
      FROM [Book] AS [b]
      WHERE [b].[AuthorID] = @id]
```

\* De ce `OrderBy(b => b.Author.FullName)` aruncă o excepție? \\\[ pentru ca FullName e doar o proprietate calculata si nu exista in baza de date]
\* Cum am rezolvat: \\\[ am facut sortarea dupa LastName]



## S3 · Filtrul variantei mele (V3)

\* Filtrul implementat: \*\*\\\[ autor]\*\*
\* Interogarea , cu filtrul aplicat: am selectat un autor din lista si am introdus in bara de cautare ce sa contina titlul

```sql
\\\[]
```

## S4 · Folosirea AI

\* \*\*Instrumente folosite:\*\* \\\[Gemini]
\* \*\*La ce pași:\*\* \\\[ s1,s2, s3]
\* \*\*Un exemplu de cerere trimisă:\*\*

> \\\[cum adaug filtru dupa autor in books/index astfel incat sa se combine cu cautarea dupa titlu si sortare]

\* \*\*O situație în care am corectat sau respins un răspuns AI\*\* (sau, dacă nu am folosit AI, o eroare întâlnită și cum am găsit-o):

  \* Ce a propus AI-ul / care era eroarea: \\\[ pentru filtrarea din controller nu  adougat parametrul suthorId in semnatura metodei ]
  \* Cum mi-am dat seama că e greșit sau ineficient: \\\[ lista de erori la compilare ]
  \* Codul inițial:

```csharp
\\\[public async Task<IActionResult> Index(string? sortOrder, string? currentFilter, string? searchString, int? pageNumber) ]
```

\* Codul final:

```csharp
\\\[ public async Task<IActionResult> Index(string? sortOrder, string? currentFilter, string? searchString, int? authorId, int? pageNumber)]
```

\\---

## Ce nu am reușit (opțional)

\\\[]



LABORATOR 5

Pragul pentru cold start: 15
Al doilea grafic din S2: media pe gen

A1: Au fost importate 6599 evaluări.

A2: dupa reantrenare am obtinut RMSE = 0.70
    am accesat recomandarile pentru userId = 5 si s-a afisat topul recomandarilor si scorul acestora

A3: pentru cititorul 5 avem lista recomandarilor pe baza istoricului de evaluari a cititorului, valoarea fallback este falsa 
    pentru cititorul 999 care nu are un istoric al evaluarilor, se declasnseaza mecanismul de cold start. lista recomandarilor se calculeaza in functie de cele mai bine evaluate carti din baza de date. Valoarea fallback este true

A4: MicroAccuracy: 74.7%   - calculeaza global si este influentata de genurile cu mai multe carti
    MacroAccuracy: 74.5%   - calculeaza media separat pentru fiecare gen; genurile cu putine carti trag media in jos

    genuri care se confunda cel mai des: poezie si nuvela, programare si sf, clasic universal si roman romanesc.
    aceasta confuzie este logica deoarece aceste genuri au in comun diferite caracteristici: vocabular folosit in descrieri, temele abordate, stilul narativ
    
A5: pentru descrierea Dune ghiceste genul sf cu incredere de 77%
    pentru descrierea "un detectiv pe o stație spațială" ghiceste genul sf cu incredere de 80%
