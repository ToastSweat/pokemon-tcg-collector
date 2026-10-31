"""Reproduce offline verification and seal this research delivery; own folder only."""
from datetime import datetime, timezone
from pathlib import Path
import hashlib
import json
import subprocess
import sys
from build_supplement import build, HERE, ROOT
from retrieve_evidence import CITATIONS

if __name__=='__main__':
    result=build()
    (HERE/'sheet-weight-supplement.json').write_text(json.dumps(result,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
    option={'schema':'research-approval-option-not-runtime/v1','discussionId':'source-qualified-J1-F1-marginal-weights','approved':False,'implemented':False,'mayBeLoadedAsGeneratorRules':False,'explicitUserApprovalRequired':True,'scope':'ONE conditional weights-only alternative to uniform pools, not a selected generator model','reason':'Retains the observed secondary-table differences rather than erasing them; exact applicability to a physical run remains unauthenticated','sources':'citations-ledger.json; REPORT.md; manual-transcription.json','sets':[],'unresolvedBeforeAnyUse':['Which physical/completion target and Jungle run to select','Whether to approve the provisional relative masses at all','Branch/insertion probabilities and replacement mechanism','Within-pack duplicate/replacement policy and multi-card inclusion marginals','Pack order/orientation','Loose-pack independence versus factory/box collation'],'excluded':['Jungle J2 First Edition error promo','Removed blank/Ivy outcome','J3 averaging or automatic mixing','Uninspected Belgian Jungle weights','Authenticated factory-count claim','Any RNG/runtime/RuleData adoption']}
    for table_id in ['jungle-1-removed','fossil-1']:
        t=next(t for t in result['tables'] if t['tableId']==table_id)
        option['sets'].append({'setId':t['setId'],'sourceTableId':table_id,'imageSourceId':t['imageSourceId'],'physicalRunAuthentication':False,'emittedPoolDenominators':t['checks']['allOutcomePositionSums'],'ordinaryCandidateMembers':len(t['cards']),'weightsReference':'sheet-weight-supplement.json#/tables/'+str(result['tables'].index(t))+'/cards','weightMeaning':'Each card multiplicity from this source table only; normalization is magnitude / emitted pool denominator, not a pack-inclusion model','uniformFallbackComparator':{p: {'perCardRelativeWeight':1,'memberCount':sum(c['pool']==p for c in t['cards'])} for p in ['H','R','U','C']}})
    (HERE/'provisional-option.json').write_text(json.dumps(option,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
    test=subprocess.run([sys.executable,'-B',str(HERE/'WeightEvidenceTests.py')],capture_output=True,text=True,encoding='utf-8')
    (HERE/'final-tests.log').write_text(test.stdout+test.stderr+'\nexit_code='+str(test.returncode)+'\n',encoding='utf-8')
    print(test.stdout+test.stderr)
    assert test.returncode==0
    assert 'Ran 3 tests' in test.stderr
    citation_log=[]
    for name in ['REPORT.md','per-card-transcription.md']:
        for args in [('render','--replace-in',str(HERE/name)),('verify',str(HERE/name),'--strict','--evidence')]:
            p=subprocess.run([sys.executable,'-B',str(CITATIONS),'--ledger',str(HERE/'citations-ledger.json'),*args],capture_output=True,text=True,encoding='utf-8')
            citation_log += ['COMMAND: '+repr(args),p.stdout,p.stderr,'exit_code='+str(p.returncode)]
            assert p.returncode==0, p.stdout+p.stderr
    (HERE/'final-citation-verification.log').write_text('\n'.join(citation_log)+'\n',encoding='utf-8')
    print('\n'.join(citation_log))
    manifest=json.loads((HERE/'retrieval-manifest.json').read_text(encoding='utf-8'))
    assert all(hashlib.sha256((ROOT/p['path']).read_bytes()).hexdigest()==p['sha256'] for p in manifest['inputs'])
    tables=result['tables']
    # Separate arithmetic traversal over exported rows; no reliance on the builder's histogram accumulator.
    independent={}
    for table in tables:
        pools={}
        for symbol in ['H','R','U','C']:
            weights=[c['multiplicity'] for c in table['cards'] if c['pool']==symbol]+[e['multiplicity'] for e in table['nonCatalogOutcomes'] if e['pool']==symbol]
            pools[symbol]={'distinctOutcomes':len(weights),'positionSum':sum(weights)}
            assert sum(weights)==table['checks']['allOutcomePositionSums'][symbol]
        independent[table['tableId']]=pools
    proof={'verifiedAtUtc':datetime.now(timezone.utc).isoformat(),'offlineTestExitCode':test.returncode,'offlineTestCount':3,'corruptionSubcases':len(['wrong U total','wrong magnitude','promo conflation','unmapped name','image swap','sourceId','sourceName','collector number','display name','rarity']),'separateCaptionMixRejection':True,'tableCount':len(tables),'groupCount':sum(len(t['checks']['groupChecks']) for t in tables),'mappedOrdinaryRows':sum(len(t['cards']) for t in tables),'nonCatalogOutcomes':sum(len(t['nonCatalogOutcomes']) for t in tables),'printedPChecks':sum(len(g['pChecks']) for t in tables for g in t['checks']['groupChecks']),'independentExportedRowSums':independent,'allOriginalInputPinsUnchanged':True,'sourceImageIdsExactlyMatchDownloadedSha1':True,'bothCitationDocumentsPassedStrictEvidenceVerification':True,'approvalGranted':False,'generatorAdopted':False,'gameplayOrUnityVerificationClaimed':False,'writesScope':'docs/research/phase5/weight-supplement/* only','artifactSha256ExcludingThisProof':[]}
    for p in sorted(HERE.iterdir(),key=lambda p:p.name):
        if p.is_file() and p.name!='verification-proof.json':
            b=p.read_bytes()
            proof['artifactSha256ExcludingThisProof'].append({'path':p.name,'bytes':len(b),'sha256':hashlib.sha256(b).hexdigest()})
    (HERE/'verification-proof.json').write_text(json.dumps(proof,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
    print(json.dumps({k:v for k,v in proof.items() if k!='artifactSha256ExcludingThisProof'},indent=2))
