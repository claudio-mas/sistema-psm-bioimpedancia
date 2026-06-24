from pathlib import Path
import zipfile, xml.etree.ElementTree as ET, json
root=Path(r'C:\Sistema\PSM\Projeto 14\zanoni')
out=root/'_prd_evidence'
out.mkdir(exist_ok=True)
ns={'m':'http://schemas.openxmlformats.org/spreadsheetml/2006/main','r':'http://schemas.openxmlformats.org/officeDocument/2006/relationships'}
z=zipfile.ZipFile(root/'Bioimpedancia.xlsm')
wb=ET.fromstring(z.read('xl/workbook.xml'))
rels=ET.fromstring(z.read('xl/_rels/workbook.xml.rels'))
ridmap={rel.attrib['Id']:rel.attrib['Target'] for rel in rels}
shared=[]
if 'xl/sharedStrings.xml' in z.namelist():
    ss=ET.fromstring(z.read('xl/sharedStrings.xml'))
    for si in ss.findall('m:si',ns): shared.append(''.join(t.text or '' for t in si.findall('.//m:t',ns)))
sheets=[]; formulas=[]; samples=[]
for sh in wb.find('m:sheets',ns):
    name=sh.attrib['name']; rid=sh.attrib['{http://schemas.openxmlformats.org/officeDocument/2006/relationships}id']
    target=ridmap[rid].lstrip('/')
    path='xl/'+target if not target.startswith('xl/') else target
    xml=ET.fromstring(z.read(path))
    dim=xml.find('m:dimension',ns); rows=xml.findall('.//m:row',ns); fc=0; non=0; sample=[]
    for row in rows[:5000]:
        vals=[]
        for c in row.findall('m:c',ns):
            ref=c.attrib.get('r'); f=c.find('m:f',ns); v=c.find('m:v',ns); val=None
            if v is not None: val=v.text
            if c.attrib.get('t')=='s' and val is not None and val.isdigit() and int(val)<len(shared): val=shared[int(val)]
            if f is not None: fc+=1; formulas.append({'sheet':name,'cell':ref,'formula':'='+('' if f.text is None else f.text),'value':val})
            if f is not None or val not in (None,''):
                non+=1
                if len(vals)<20: vals.append(f'{ref}={f.text if f is not None else val}')
        if vals and len(sample)<100: sample.append(' | '.join(vals))
    sheets.append({'sheet':name,'path':path,'dimension':dim.attrib.get('ref') if dim is not None else None,'rows_xml':len(rows),'nonempty_scanned':non,'formulas':fc})
    samples.append({'sheet':name,'sample':sample})
(out/'xml_summary.json').write_text(json.dumps(sheets,ensure_ascii=False,indent=2),encoding='utf-8')
(out/'xml_formulas.json').write_text(json.dumps(formulas,ensure_ascii=False,indent=2),encoding='utf-8')
(out/'xml_samples.json').write_text(json.dumps(samples,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(sheets,ensure_ascii=False,indent=2)); print('formulas',len(formulas))
