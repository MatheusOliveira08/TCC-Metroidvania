# Diretrizes de Código para as IAs (Unity C#)

Você está atuando como Engenheiro de Software Sênior em Unity. Suas respostas devem conter apenas o código necessário e as instruções de onde colocá-lo, sem explicações teóricas longas, a menos que solicitado.

## Regras de Arquitetura Unity

- Use `Rigidbody2D` e `BoxCollider2D` para físicas. Não use componentes 3D.
- Mantenha os scripts modulares. Separe a captura de inputs da lógica de física.
- Evite o uso excessivo do `Update()`. Prefira `FixedUpdate()` para físicas e eventos dirigidos (Events/Delegates) para comunicação entre scripts.
- O código deve ser limpo, comentado onde houver lógica complexa, e seguir o padrão de nomenclatura C# (PascalCase para classes e métodos, camelCase para variáveis privadas).

## TDD e Testes

- O objetivo do nosso desenvolvimento é ser prático. Foque o TDD apenas no que é essencial para não poluir o repositório.
- Mantenha testes permanentes no sistema apenas para features realmente importantes (ex: a lógica principal da geração dos dados de proveniência ou a API base do jogo).
- Caso a feature seja mais simples e você queira criar um teste temporário apenas para facilitar a implementação e deixar o código mais seguro, você pode fazê-lo. Crie o teste, implemente a lógica e, depois de validar, **apague o teste**.
- Caso enxergue um motivo vital para salvar um teste de uma feature simples, você deve me perguntar primeiro e me dar a sua justificativa.
