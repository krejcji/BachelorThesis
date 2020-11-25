# MAPF for pickers in a warehouse

## Progress report



**25.11.**

* Snížení paměťové náročnosti prohledávání
  * Efektivní implementace třídy Tour

**11.11.**

* Dokončeno parsování
* Předběžné testy

**4.11.**

* První funkční verze generování instancí v C# a návrh parametrizace
* Implementována kostra testovacího frameworku
* Změny architektury

**20.10.**

* Přepsání generování instancí do C# pro zefektivnění budoucích testů
  * zatím pouze základní parametrizace dle velikosti, do budoucna složitější
* Serializace Excelových instancí do souboru, parsing v jazyce C#

**14.10.**

* Refactoring
* Parsing Excel souborů a výběr důležitých dat pro projekt

#### 8.7

* Práce na textu - 3. kapitola
* Implementace Priority planning přístupu
  * předběžné výsledky ukazují, že s trochou optimalizace by mohl tento přístup stačit i na velké instance
* CBS picking conflict
  * zatím si myslím, že nelze řešit jinak, než naivně jednou podmínkou na jedno dělení bez ztráty některých řešení
    * problém je v tom, že nemůžeme předpokládat, že neomezený agent bude na daném vrcholu opravdu "pickovat" i v budoucích řešeních, v případě intervalové podmínky po dobu pickování by se některé podmínky druhého agenta mohly stát nadbytečnými
  * alternativně by šlo dělit syny na vynucené pickování (vrchol,čas) a zakázané (vrchol,čas), to ale neřeší postupné větvení na (vrchol,čas+1),.. atd.
  * pravděpodobně tento typ konfliktů ani nebude způsobovat velké větvení, protože i při prodloužení cesty o 1 se najde zcela jiná cesta
* CBS corridor conflict 
  * podobný problém jako u picking conflictů
  * agenti málokdy chtějí pouze projít

#### 1.7.

* Práce na textu - 1.,2. a 3. kapitola

#### 24.6.

* Implementace CBS
  * zatím pouze základní algoritmus bez rozšíření

#### 17.6.

* Implementace algoritmu pro GTSP
  * prohledávání stavového prostoru
  * matice #pick lokací X #maximální čas
    * na každé pozici je uloženo, z jakých podmnožin tříd se na danou pozici lze dostat
    * exponenciální v počtu tříd - 2^n možných podmnožin
  * po nalezení cílového vrcholu je díky množinám možné zpětně dohledat nějakou cestu
* Začátek implementace CBS
  * pro low-level prohledávání chci použít svůj GTSP prohledávací algoritmus

#### 10.6.

* Generování testovacích instancí
  * 3 velikosti   (# uliček x (#bloků x délka bloku) x výška
    1. 20 x (2 x 30) x 5          pick lokací  - 6000
    2. 40 x (4 x 30) x 5          lokací          - 24000
    3. 80 x (5 x 30) x 5          lokací          - 60000
  * Random storage/ Class-based storage - ABC
* Algoritmy - vyzkoušené
  * GTSP Solver - GLNS
    * převod instance do GTSP - time-expanded graph
    * špatně škáluje na větši instance
      * popis problému matice sousednosti
    * heuristický solver nezvládl najít ani přípustnou cestu
      * není optimalizovaný na DAG
* Algoritmy - možné přístupy
  * ICTS 
    * enumerace všech cest délky x?
    * škálování v počtu pickerů?
  * CBS
    * blokování hrany v určitém čase
    *  nutnost time-expanded grafu
      * aspoň v časech, kdy existují omezení
    * corridor conflicts resolution
  * BCP
    * také time-expanded graf
    * potenciálně rychlejší výpočet