# Prompt pro GitHub Copilot – webová stránka o algoritmech

> Tento prompt byl vytvořen na základě výukového souboru [`algo1.md`](algo1.md). Vložte jej do GitHub Copilot Chat (režim Agent) v kořenové složce projektu.

## 1. Role a cíl

Jsi zkušený front-end vývojář a učitel informatiky. Vytvoř **jednu česky psanou webovou stránku `algoritmy.html`** na téma **Algoritmizace**, která žákům střední školy srozumitelně vysvětlí, co je algoritmus, jaké vlastnosti musí mít, jak se zapisuje a jak se kreslí vývojový diagram. Součástí stránky bude **jednoduchý test** s vyhodnocením.

## 2. Zdroj obsahu

- Veškerý odborný obsah čerpej **výhradně ze souboru `algo1.md`** (text a testové otázky včetně klíče správných odpovědí). Nic si nedomýšlej a neměň význam.
- Kapitoly zdroje, které musí stránka pokrýt:
  1. **Co je algoritmus a proč ho vytváříme** – dvě fáze tvorby programu (vymyšlení postupu, přepis do programovacího jazyka) a definice algoritmu.
  2. **Vlastnosti správného algoritmu** – konečnost, věcná správnost, jednoznačnost, obecnost, opakovatelnost (determinismus), srozumitelnost. U věcné správnosti uveď zápis zlomku se závorkami `X := (A+B) / (C+D)` a funkci `SQRT(...)`; u jednoznačnosti podmínky řešitelnosti (dělení nulou, záporný výraz pod odmocninou).
  3. **Možnosti zápisu algoritmů** – tabulka: slovní vyjádření, matematický zápis, rozhodovací tabulky, vývojové diagramy, počítačový program (použití, výhody, nevýhody).
  4. **Základní značky vývojových diagramů** – mezní značka, vstup/výstup, zpracování, rozhodovací blok, příprava, spojka, podprogram. Směr kreslení: odshora dolů a zleva doprava.
- Přidej **vlastní jednoduchý ilustrační vývojový diagram** (např. dělení dvou čísel s ošetřením dělení nulou) nakreslený jako **inline SVG**. Nekopíruj obrázky z knih ani z internetu.

## 3. Struktura stránky

1. Hlavička s logem a navigací (Domů, Algoritmy, Test, Hledat).
2. Úvodní sekce s nadpisem `h1` a odkazem na test.
3. Sekce *Co je algoritmus*.
4. Sekce *Vlastnosti algoritmu* – šest karet.
5. Sekce *Možnosti zápisu* – přístupná tabulka (`caption`, `th scope`).
6. Sekce *Vývojové diagramy* – značky (SVG) + ilustrační diagram.
7. Sekce *Test* – viz níže.
8. Patička s odkazy na validátory a citace.

## 4. Test

- **5 uzavřených otázek** (jedna správná odpověď ze čtyř) převezmi z části A souboru `algo1.md`, **4 otevřené otázky** z části B zobraz jako rozbalovací `<details>` se vzorovou odpovědí.
- Každá uzavřená otázka je `<fieldset>` s `<legend>` a čtyřmi `<input type="radio">` s propojenými `<label>`.
- Tlačítko **Vyhodnotit** zobrazí skóre (např. „Správně 4 z 5“), u každé otázky označí správnou/špatnou odpověď a ukáže krátké vysvětlení z klíče. Tlačítko **Vymazat test** vrátí formulář do původního stavu.
- Vyhodnocení napiš v **čistém JavaScriptu** (bez knihoven) do samostatného souboru `algoritmy.js`. Výsledek ohlas přes `aria-live="polite"`. Stav nesmí být vyjádřen jen barvou (přidej text/ikonu).

## 5. Technické požadavky

- **Validní HTML5** (doctype, `lang="cs"`, `meta charset`, `meta viewport`, jedinečná `id`, žádné zastaralé elementy a atributy). Stránka musí projít kontrolou <https://validator.w3.org/> **bez chyb a varování**.
- **Validní CSS3** – styly přidej do existujícího `style.css` a drž se vzhledu stávajícího webu (tmavé pozadí `#080b12`, akcent `#6ce8ed`, písma Manrope a DM Mono). CSS musí projít <https://jigsaw.w3.org/css-validator/> bez chyb.
- Responzivní návrh (od šířky 360 px), čitelný kontrast, viditelný `:focus-visible`, respektuj `prefers-reduced-motion`.
- Sémantické značky (`header`, `nav`, `main`, `section`, `footer`), jeden `h1`, logická hierarchie nadpisů, `alt`/`aria-label` u grafiky.
- Bez frameworků a externích skriptů. Stránka musí fungovat na GitHub Pages.

## 6. Povinné prvky projektu

- Na stránce jsou **dva odkazy na validaci** – HTML (`https://validator.w3.org/`) a CSS (`https://jigsaw.w3.org/css-validator/`) – nastavené tak, aby validovaly kód dané stránky (stejné řešení jako v `index.html`: znovu použij `validators.js`).
- Na stránce je odkaz na soubor **`citace.md`**.
- Soubor **`citace.md`** doplň o **použité prompty** (tento prompt i původní zadání) a o všechny **zdroje** (`algo1.md`, učebnice, validátory, písma).
- Nově vytvořenou stránku propoj s ostatními stránkami webu (odkaz v hlavní navigaci).

## 7. Kritéria přijetí

- [ ] Stránka obsahuje všechny čtyři kapitoly z `algo1.md`.
- [ ] Test obsahuje 5 uzavřených a 4 otevřené otázky a správně se vyhodnotí.
- [ ] HTML i CSS jsou validní.
- [ ] Existuje `citace.md` s prompty a zdroji a je z webu dostupný.
- [ ] Stránka je responzivní a ovladatelná klávesnicí.
- [ ] Obsah je věcně shodný se zdrojovým souborem.
