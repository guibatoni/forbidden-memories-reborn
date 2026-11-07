# Prototype 0.4.2.1 — correção do ciclo de vida das partículas

## Problema observado

Na versão 0.4.2, o Unity 6.3 LTS (`6000.3.28f1`) registrava repetidamente:

`Setting the duration while system is still playing is not supported.`

O rastreamento apontava para `AttackProjectile.CreateImpact()` em `AttackProjectile.cs:71`.

## Causa e correção

Quando um `ParticleSystem` é adicionado a um `GameObject` ativo, sua reprodução pode iniciar automaticamente. A propriedade `ParticleSystem.MainModule.duration` só pode ser alterada depois de interromper a reprodução.

Inserimos `Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear)` imediatamente após `AddComponent<ParticleSystem>()` **antes** de configurar `main.duration`, `playOnAwake` e os demais parâmetros.

Três pontos do código foram padronizados:

1. `AttackProjectile.CreateImpact()` — partículas do impacto do ataque (causa reportada).
2. `ArenaFactory.PlaySandboxRemoval()` — efeito demonstrativo de remoção.
3. `ArenaFactory.PlayMatchDestruction()` — efeito de destruição por combate.

As partículas voltam a ser iniciadas explicitamente por `Play()` após a configuração. Não alteramos a câmera, os 5 slots de monstros e 5 de suporte por lado nem as regras do duelo.

## Validação no Unity (pendente)

1. Instale o patch **com o Unity Editor fechado**.
2. Abra com Unity 6.3 LTS `6000.3.28f1`, entre em **Play** e comece o duelo experimental.
3. Jogue até oponente e jogador realizarem vários ataques (diretos e contra monstros), observando o Console.
4. Valide que a mensagem `Setting the duration while system is still playing` não reaparece.
5. Confirme que rastro, impacto e destruição ainda são exibidos.

Os testes do GitHub Actions cobrem o **núcleo C# puro**, não a reprodução de partículas no Unity Editor. Portanto, este bug só será considerado validado após seu teste visual.
