# CVAnalysisHub Documentation

Тази папка съдържа цялата документация за курсовия проект:

- `main.tex` - главният LaTeX файл
- `preamble.tex` - общи пакети, макроси и стил
- `chapters/` - отделните части на документацията
- `figures/diagrams/` - PlantUML източниците
- `figures/generated/` - експортираните изображения от PlantUML
- `figures/branding/` - лого на ТУ-София и други branding assets
- `figures/screenshots/` - по желание screenshots от приложението
- `build/` - генерираният PDF и временни build файлове

## Препоръчителен workflow във VSCode

1. Отвори папката `CVAnalysisHub/documentation`.
2. Използвай LaTeX extension и за root файл избери `main.tex`.
3. Рендерирай PlantUML диаграмите от `figures/diagrams/` към `figures/generated/`.

Ако отвориш директно `documentation/` като VSCode workspace folder, приложените настройки в `.vscode/settings.json` вече задават:

- LaTeX output папка: `build/`
- PlantUML source папка: `figures/diagrams/`
- PlantUML export папка: `figures/generated/`

Препоръчителни имена на generated файловете:

- `figures/generated/db-schema.png`
- `figures/generated/architecture-overview.png`
- `figures/generated/mvvm-mapping.png`
- `figures/generated/analysis-flow.png`

За корицата можеш да сложиш логото от приложеното изображение като:

- `figures/branding/tu-sofia-logo.png`

След това build-вай `main.tex`.

## PlantUML export във VSCode

След като папката `documentation/` е отворена като workspace folder:

1. Отвори `.puml` файл от `figures/diagrams/`.
2. Използвай `PlantUML: Export Current Diagram`.
3. Избери `png`.
4. Extension-ът трябва да използва зададената `plantuml.exportOutDir` стойност и да запише файла в `figures/generated/`.

Ако extension-ът все пак поиска ръчен път, запази с точното име:

- `figures/generated/db-schema.png`
- `figures/generated/architecture-overview.png`
- `figures/generated/mvvm-mapping.png`
- `figures/generated/analysis-flow.png`

## Локален build без latexmk

В текущата машина има `pdflatex`, но няма `latexmk` и `plantuml` CLI. Ако искаш build през терминал:

```bash
cd documentation
pdflatex -interaction=nonstopmode -output-directory=build main.tex
pdflatex -interaction=nonstopmode -output-directory=build main.tex
```

PDF-ът ще бъде в:

`documentation/build/main.pdf`

## Забележка за диаграмите

LaTeX шаблонът е направен така, че да build-ва и без generated diagram images. Ако изображението липсва, вместо него ще се покаже placeholder рамка. Това ти позволява:

- първо да пишеш текста;
- после да рендерираш диаграмите;
- без документът да спира да се компилира.
