"""Expand visually transcribed secondary chart groups into research-only card maps.
No game imports, generation, simulation, RNG, asset writes, or rules publication.
"""
from pathlib import Path
from collections import Counter
from datetime import datetime, timezone
from fractions import Fraction
import hashlib
import json
import re
import struct

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
RARITIES = {'H':'holo','R':'rare','U':'uncommon','C':'common'}
SOURCES = {'jungle':'docs/research/phase5/jungle/tcg-data.raw.json', 'fossil':'docs/research/phase5/fossil/evidence/api.json'}

def load(path):
    return json.loads(path.read_text(encoding='utf-8'))

def frac(value):
    return {'numerator':value.numerator,'denominator':value.denominator}

def build(transcription=None, catalogs=None):
    data = transcription if transcription is not None else load(HERE/'manual-transcription.json')
    catalogs = catalogs if catalogs is not None else {s:load(ROOT/f'Authoring/Phase5/Data/{s}-en-unlimited1999.catalog.json') for s in SOURCES}
    raw = (ROOT/'docs/research/phase5/jungle/rarity-guide.raw.html').read_text(encoding='utf-8')
    manifest = load(HERE/'retrieval-manifest.json')
    for pin in manifest['inputs']:
        assert hashlib.sha256((ROOT/pin['path']).read_bytes()).hexdigest()==pin['sha256'], f"input pin changed: {pin['path']}"
    expected_bindings = [
        ('jungle-1-removed','jungle','c10b7cdcae9c7f888eea6b72108aca993c11c6bc','1) 1st Edition & Unlimited',2),
        ('jungle-2-ivy-error','jungle','3a4f6ceeac70e776f92552a6b0097d6c62e3ac4e','2) Error version (1st Edition)',3),
        ('jungle-3-exeggutor','jungle','412a4015bff1ef1eb12453acbc355cf7448ab776','3) Unlimited (new uncommons sheet)',4),
        ('fossil-1','fossil','18d87beedd4eb026e3af510c22670f09e3d2ce30','1) 1st Edition & Unlimited',5),
    ]
    actual_bindings = [(t['tableId'],t['setId'],t['imageSourceId'],t['titleVerbatim'],t['citationId']) for t in data['tables']]
    assert actual_bindings==expected_bindings, 'caption/variant/imageSourceId binding mismatch'
    images = {r['imageSourceId']:r for r in manifest['charts']}
    output = []
    inherited = {}
    for t in data['tables']:
        sid = t['imageSourceId']
        assert sid in images, 'unexpected imageSourceId'
        image = images[sid]
        imgbytes = (HERE/(sid+'.png')).read_bytes()
        success = next(a for a in image['attempts'] if a['success'])
        assert hashlib.sha256(imgbytes).hexdigest()==success['sha256']
        assert hashlib.sha1(imgbytes).hexdigest()==sid, 'literal imageSourceId != downloaded SHA1'
        assert struct.unpack('>II',imgbytes[16:24])==(success['width'],success['height'])
        jungle_start = raw.index('<p><strong>Jungle</strong></p>')
        fossil_start = raw.index('<p><strong>Fossil</strong></p>')
        fossil_end = raw.index("<div id='post_5'",fossil_start)
        image_pos = raw.index('href="'+image['suppliedUrl']+'"',jungle_start if t['setId']=='jungle' else fossil_start)
        if t['setId']=='jungle':
            assert jungle_start<image_pos<fossil_start, 'Jungle caption/source mismatch'
        else:
            assert fossil_start<image_pos<fossil_end, 'Fossil caption/source mismatch'
        cat = catalogs[t['setId']]
        src = load(ROOT/SOURCES[t['setId']])
        assert isinstance(src,list)
        smap = {x['number']:x for x in src}
        assert len(smap)==len(src)==len(cat['cards'])
        assert sorted(int(n) for n in smap)==list(range(1,len(src)+1))
        assert len({c['collectorNumber'] for c in cat['cards']})==len(src)
        assert len({c['id'] for c in cat['cards']})==len(src)
        for c in cat['cards']:
            n = c['collectorNumber']
            assert re.fullmatch(r'[1-9][0-9]*',n), 'noncanonical collector number'
            s = smap[n]
            assert c['sourceId']==s['id']
            assert c['sourceName']==s['name']
            display = 'Nidoran♀' if s['name']=='Nidoran ♀' else s['name']
            assert c['displayName']==display
            rarity = {'Rare Holo':'holo','Rare':'rare','Uncommon':'uncommon','Common':'common'}[s['rarity']]
            assert c['rarity']==rarity
            assert c['finish']==('holo' if rarity=='holo' else 'nonholo')
            assert c['kind']=={'Pokémon':'pokemon','Trainer':'trainer'}[s['supertype']]
            assert c['printingId']==cat['printingId']
            assert c['setId']==t['setId']
            assert c['language']=='en' and c['edition']=='unlimited' and c['variant']=='standard'
        cards = []
        noncatalog = []
        group_checks = []
        all_sums = {}
        candidate_sums = {}
        for p in t['pools']:
            symbol = p['symbol']
            members = [c for c in cat['cards'] if c['rarity']==RARITIES[symbol]]
            names = [c['displayName'] for c in members]
            assert len(set(names))==len(names)
            lines = p['exceptionLinesVerbatim']
            if p['exceptionNameInheritance']:
                assert t['footerVerbatim']=='(other sheets assumed unchanged)'
                lines = inherited[symbol]
            elif t['tableId']=='jungle-1-removed':
                inherited[symbol] = lines
            exceptions = {}
            for line in lines:
                label, literals = line.split('    ',1)
                assert label[0]==symbol
                m = int(label[1:])
                for literal in literals.split(', '):
                    name = 'Nidoran♀' if literal=='Nidoran f' else literal
                    if literal=='Pikachu (Promo)':
                        assert t['tableId']=='jungle-2-ivy-error' and symbol=='U'
                        noncatalog.append({'label':literal,'pool':symbol,'multiplicity':m,'catalogId':None,'eligibleForCandidate':False,'reason':'First Edition error-variant promo only; no Jungle collector-number mapping asserted'})
                    else:
                        assert name in names, f'unmapped chart name: {literal}'
                        assert name not in exceptions
                        exceptions[name] = (m,literal)
            named_m = {m for m,_ in exceptions.values()}
            if symbol=='U' and t['tableId']=='jungle-2-ivy-error':
                named_m.add(1)
            complements = {g['m'] for g in p['groups']}-named_m
            assert len(complements)==1, 'ambiguous complementary magnitude'
            default_m = complements.pop()
            denominator = p['emittedPositionTotal']
            counts = Counter()
            candidate_sum = 0
            for c in members:
                m = exceptions[c['displayName']][0] if c['displayName'] in exceptions else default_m
                counts[m]+=1
                candidate_sum+=m
                prob = Fraction(m,denominator)
                uniform = Fraction(1,len(members))
                row = {k:c[k] for k in ['id','sourceId','collectorNumber','displayName','sourceName','rarity','finish','kind']}
                row.update(pool=symbol,multiplicity=m,imageSourceId=sid,citationId=t['citationId'],authorPoolProbability=frac(prob),uniformCandidateProbability=frac(uniform),ratioToUniformCandidate=frac(prob/uniform),assignmentBasis='named exception' if c['displayName'] in exceptions else 'complement of named exception within this catalog rarity',sourceNameLiteral=exceptions[c['displayName']][1] if c['displayName'] in exceptions else None,assignmentInheritedFrom=p['exceptionNameInheritance'])
                cards.append(row)
            for extra in noncatalog:
                if extra['pool']==symbol:
                    counts[extra['multiplicity']]+=1
            expected_counts = Counter({g['m']:g['n'] for g in p['groups']})
            assert counts==expected_counts, f'per-card histogram disagrees with pixels: {t["tableId"]}/{symbol}'
            distinct = sum(counts.values())
            positions = sum(m*n for m,n in counts.items())
            assert distinct==p['distinctCardTotal'] and positions==denominator
            # Independent row-by-row sum, not just copying the printed denominator.
            row_sum = sum(c['multiplicity'] for c in cards if c['pool']==symbol)+sum(e['multiplicity'] for e in noncatalog if e['pool']==symbol)
            assert row_sum==positions
            all_sums[symbol]=positions
            candidate_sums[symbol]=candidate_sum
            insertion = Fraction(t['sequenceInsertions'][symbol],t['sequenceLength'])
            pchecks = []
            for g,pv in zip(p['groups'],p['pRows'],strict=True):
                assert g['n']==pv['n']
                calculated = Fraction(denominator,1)/(insertion*g['m'])
                printed = Fraction(pv['p'])
                assert abs(calculated-printed)<=Fraction(1,20), 'printed p not rounded consistently'
                pchecks.append({'n':g['n'],'m':g['m'],'printedP':pv['p'],'derivedReciprocalExpectedOccurrences':frac(calculated),'withinHalfTenth':True})
            group_checks.append({'pool':symbol,'histogram':[{ 'm':m,'n':counts[m]} for m in sorted(counts,reverse=True)],'distinctTotal':distinct,'positionTotal':positions,'candidateOnlyPositionSum':candidate_sum,'printedGroupAgreement':True,'independentRowSumAgreement':True,'pChecks':pchecks})
        assert len(cards)==len(cat['cards'])
        assert sum(p['distinctCardTotal'] for p in t['pools'])==t['setSize']
        assert sum(t['sequenceInsertions'].values())==t['nseq']==t['np']*t['sequenceLength']
        assert sum(Fraction(n,t['sequenceLength']) for n in t['sequenceInsertions'].values())==t['np']
        output.append({'tableId':t['tableId'],'imageSourceId':sid,'titleVerbatim':t['titleVerbatim'],'physicalPrintingAuthenticated':False,'setId':t['setId'],'applicability':'First Edition error variant only; not eligible for the Unlimited candidate' if t['tableId']=='jungle-2-ivy-error' else 'Catalog name/number/rarity map compatible; exact physical run not authenticated','cards':sorted(cards,key=lambda c:int(c['collectorNumber'])),'nonCatalogOutcomes':noncatalog,'checks':{'exactCatalogAndSourceMap':True,'imageSourceIdAndArticlePlacement':True,'allOutcomePositionSums':all_sums,'candidateOnlyPositionSums':candidate_sums,'groupChecks':group_checks,'totalDistinctOutcomes':sum(p['distinctCardTotal'] for p in t['pools']),'sequenceArithmeticChecked':True}})
    actual_order = [raw.index('href="'+images[t['imageSourceId']]['suppliedUrl']+'"',raw.index('<p><strong>Jungle</strong></p>')) for t in data['tables']]
    assert actual_order==sorted(actual_order), 'article/chart ordering changed'
    return {'schema':'secondary-sheet-weight-research/v1','generatedAtUtc':datetime.now(timezone.utc).isoformat(),'status':'UNAPPROVED: evidence supplement only; not RuleData or generator configuration','authenticatedFactoryCounts':False,'computedFieldDefinitions':{'authorPoolProbability':'DERIVED multiplicity / transcribed emitted pool total, including the noncatalog promo for the error table; not a measured probability or inclusion probability in a multi-card pack','uniformCandidateProbability':'DERIVED 1 / ordinary candidate rarity-pool member count; comparator only, not adopted','ratioToUniformCandidate':'DERIVED authorPoolProbability / uniformCandidateProbability; does not authorize renormalizing away the error-table promo','pChecks':'DERIVED reciprocal expected occurrences using the source sequence insertion ratio; verifies printed decimal rounding, not physical pack occurrence probability'},'transcriptionSource':'manual-transcription.json','tableCount':len(output),'totalMappedRows':sum(len(t['cards']) for t in output),'tables':output}

if __name__=='__main__':
    result = build()
    (HERE/'sheet-weight-supplement.json').write_text(json.dumps(result,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
    print(json.dumps({'tableCount':result['tableCount'],'totalMappedRows':result['totalMappedRows'],'sums':{t['tableId']:t['checks']['allOutcomePositionSums'] for t in result['tables']}},indent=2))
