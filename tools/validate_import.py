#!/usr/bin/env python3
"""Valida metadados de um export local, sem tocar em ROM/ISO ou enviar dados a rede.

Exemplos:
  python tools/validate_import.py caminho/para/original_export.json
  python tools/validate_import.py caminho/para/original_export.json --complete
"""
import json
import pathlib
import re
import sys

VALID_KINDS = {"monster", "spell", "trap", "equip", "ritual"}
VALID_STARS = {"mercury", "sun", "moon", "venus", "mars", "jupiter", "saturn", "uranus", "pluto", "neptune"}


def validate(data):
    errors = []
    if data.get('schema') != 1:
        errors.append('schema precisa ser 1')
    source = data.get('source') or {}
    edition = source.get('edition')
    if not isinstance(edition, str) or not (edition == 'original-ps1' or edition.startswith('original-ps1-')):
        errors.append('source.edition precisa ser original-ps1 ou original-ps1-*')
    sha = source.get('discSha256') or ''
    if sha and not re.fullmatch(r'[0-9a-fA-F]{64}', sha):
        errors.append('source.discSha256 invalido')
    cards = data.get('cards') or []
    ids = set()
    for i, card in enumerate(cards):
        cid = card.get('id')
        if not isinstance(cid, int) or not 1 <= cid <= 722:
            errors.append(f'cards[{i}].id fora de 1..722')
        if cid in ids:
            errors.append(f'ID duplicado: {cid}')
        ids.add(cid)
        if not card.get('name','').strip():
            errors.append(f'cards[{i}].name vazio')
        if str(card.get('kind','')).lower() not in VALID_KINDS:
            errors.append(f'cards[{i}].kind desconhecido')
        stars = [str(card.get(k) or '').strip().lower() for k in ('guardianStarA', 'guardianStarB')]
        if (bool(stars[0]) != bool(stars[1])) or any(star and star not in VALID_STARS for star in stars):
            errors.append(f'cards[{i}] guardian stars invalidas/incompletas')
        for attr in ('attack','defense'):
            value = card.get(attr)
            if not isinstance(value,int) or value < 0:
                errors.append(f'cards[{i}].{attr} invalido')
    if not cards:
        errors.append('lista de cartas vazia')
    fusion_keys = set()
    fusions = data.get('fusions') or []
    lookup = {c.get('id'): c for c in cards}
    for i, fusion in enumerate(fusions):
        a,b,res = (fusion.get(k) for k in ('materialA','materialB','result'))
        if type(a) is not int or type(b) is not int or type(res) is not int or \
                a not in lookup or b not in lookup or res not in lookup or \
                str(lookup[res].get('kind','')).lower() != 'monster' or fusion.get('verified') is not True:
            errors.append(f'fusions[{i}] nao validada ou referencia inexistente')
            continue
        key = tuple(sorted((a,b)))
        if key in fusion_keys:
            errors.append(f'fusions[{i}] receita duplicada')
        fusion_keys.add(key)
    rituals = data.get('rituals') or []
    ritual_ids = set()
    for i, ritual in enumerate(rituals):
        rid = ritual.get('ritualCard')
        result = ritual.get('result')
        tributes = ritual.get('tributes')
        if type(rid) is not int or type(result) is not int or \
                rid in ritual_ids or rid not in lookup or result not in lookup or \
                not isinstance(tributes, list) or len(tributes) != 3 or \
                ritual.get('verified') is not True:
            errors.append(f'rituals[{i}] receita incompleta, duplicada ou nao verificada')
            continue
        ritual_ids.add(rid)
        if str(lookup[rid].get('kind','')).lower() != 'ritual' or \
                str(lookup[result].get('kind','')).lower() != 'monster':
            errors.append(f'rituals[{i}] tipos incompatíveis')
        if any(
                type(t) is not int or t not in lookup or
                str(lookup[t].get('kind','')).lower() != 'monster'
                for t in tributes):
            errors.append(f'rituals[{i}] exige 3 monstros especificos validos')
    opponents = data.get('opponents') or []
    op_ids = set()
    for i, op in enumerate(opponents):
        oid = op.get('id')
        if not isinstance(oid, int) or oid < 0 or oid in op_ids:
            errors.append(f'opponents[{i}].id invalido ou duplicado')
        op_ids.add(oid)
        if not op.get('name','').strip():
            errors.append(f'opponents[{i}].name vazio')
    story = data.get('story') or {}
    nodes = story.get('nodes') or []
    if nodes:
        node_ids = [x.get('id') for x in nodes]
        if len(set(node_ids)) != len(node_ids) or not all(node_ids):
            errors.append('ID de evento ausente ou duplicado')
        refs = set(node_ids)
        if story.get('startNodeId') not in refs:
            errors.append('story.startNodeId nao existe nos nodes')
        for node in nodes:
            node_id = node.get('id')
            opponent_id = node.get('opponentId', 0)
            if not isinstance(opponent_id, int) or opponent_id < -1:
                errors.append(f'evento {node_id}: opponentId invalido')
                continue
            if opponent_id >= 0:
                if opponent_id not in op_ids:
                    errors.append(f'evento {node_id}: oponente {opponent_id} inexistente')
                if node.get('choices'):
                    errors.append(f'evento {node_id}: duelo nao pode ter choices')
                for key in ('winNodeId','loseNodeId'):
                    if node.get(key) not in refs:
                        errors.append(f'evento {node_id}: {key} inexistente')
            else:
                for choice in node.get('choices') or []:
                    if not choice.get('text'):
                        errors.append(f'evento {node_id}: texto de escolha vazio')
                    if choice.get('nextNodeId') not in refs:
                        errors.append(f'evento {node_id}: escolha aponta para node inexistente')
    return errors, dict(cards=len(cards), missingDescriptions=sum(not c.get('description','').strip() for c in cards),
                       opponents=len(opponents), storyEvents=len(nodes),
                       fusions=len(fusions), rituals=len(rituals),
                       descriptionCoverageByKind={
                           kind: {
                               'total': sum(str(c.get('kind','')).lower() == kind for c in cards),
                               'withText': sum(str(c.get('kind','')).lower() == kind and
                                               bool(str(c.get('description') or '').strip())
                                               for c in cards)
                           } for kind in sorted(VALID_KINDS)
                       })


def main():
    args = sys.argv[1:]
    complete = '--complete' in args
    args = [a for a in args if a != '--complete']
    if len(args) != 1:
        print(__doc__)
        return 2
    path = pathlib.Path(args[0])
    data = json.loads(path.read_text(encoding='utf-8'))
    errors, stats = validate(data)
    if complete:
        ids = [c.get('id') for c in data.get('cards', [])]
        if sorted(ids) != list(range(1, 723)):
            errors.append('catalogo original precisa conter IDs 1..722, sem lacunas')
    for key, value in stats.items():
        print(f'{key}: {value}')
    for error in errors:
        print('ERRO:',error)
    if errors:
        print('REJEITADO. Nenhum dado alterado.')
        return 1
    print('Estrutura OK. Nao confirma fidelidade aos bytes da ISO.')
    return 0


if __name__ == '__main__':
    sys.exit(main())
