# MAPF for pickers in a warehouse

## Progress report

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
    1. 10   x 10       x 5           pick lokací  - 500
    2. 20   x (2x25)x 5           lokací          - 5000
    3. 100 x (4x25) x 5          lokací          - 50000
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