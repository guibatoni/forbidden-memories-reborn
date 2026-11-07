# Prototype 0.6 — Sistema visual de cartas 2.5D

## Escopo entregue

- Nova montagem de cartas por `CardFaceBuilder`: base, borda luminosa, arte, título, categoria, faixa de atributos.
- `CardView` agora seleciona pelo único colisor no objeto raiz, sem colisor nos detalhes da moldura; o gesto de arrastar/soltar continua no mesmo ponto do sistema.
- Cartas de monstros, magias, armadilhas, equipamentos e rituais têm molduras diferenciadas e identificação visível.
- `CardArtCatalog` cria padrões geométricos originais determinísticos para cartas ainda sem arte; imagens provisórias não são as artes oficiais do Forbidden Memories.
- Preview no painel esquerdo, com nome completo, ID, tipo, atributos, Guardian Stars e descrição (quando disponível).
- Cartas viradas para baixo não expõem dados ocultos pelo painel.
- A câmera (`OrbitCamera.cs`) e posições fixas (5 monstros e 5 suportes por jogador) não foram alteradas.

## Como testar no Unity 6.3.28f1

1. Pare o modo Play, feche o editor e faça backup do projeto 0.5.
2. Extraia o patch sobre a pasta `forbidden-memories-reborn` ou abra o projeto completo 0.6.
3. Compile e abra Play. O HUD deve exibir `REBORN | 0.6`.
4. No duelo experimental, passe o mouse sobre uma carta da mão. Verifique a ampliação no **painel esquerdo** e os dados da carta.
5. Teste seleção, arraste e posicionamento de monstros e suportes nos slots corretos. Verifique que a seleção mantém o brilho na moldura.
6. Passe o turno; observe as cartas do oponente. Nenhuma carta virada para baixo deve revelar identidade pela inspeção.
7. Teste o modo Ataque/Defesa e o ataque do Prototype 0.5; veja se o combate continua normal.
8. No painel direito é possível alternar `Detalhes: ON/OFF` (ou `Cartas: detalhes ON/OFF` no fim da partida).
9. Confirme ausência de erros vermelhos no Console.

## Artes locais — opcional

Para usar imagens próprias ou devidamente licenciadas, crie a pasta `Assets/Resources/RebornCardArt` dentro do projeto Unity e coloque `ID.png` (o nome deve ser **exatamente o mesmo ID do catálogo**). Ex.: `004.png`. O Unity importa o PNG como `Texture2D` e `Resources.Load` resolve a imagem pelo ID. Sem arquivo correspondente, aparece uma arte geométrica provisória.

**Não envie artes protegidas ou ISO/ROM ao GitHub público sem autorização.** O carregador de dados originais da ISO continua limitado ao JSON validado, e nenhum dado oficial foi incluído nesta atualização.

## O que ainda falta

- Validar apresentação e desempenho dentro do Unity Editor (CI cobre núcleo C# e contratos estáticos, não renderização).
- Substituir a interface de protótipo (IMGUI) por UI Toolkit/uGUI com tipografia final, layout responsivo e preview não intrusivo.
- Importação validada das cartas e efeitos da ISO, conteúdo de campanha, modelos 3D e animações exclusivas.
- Perfis visuais de raridade/efeito provenientes de dados verificados.
