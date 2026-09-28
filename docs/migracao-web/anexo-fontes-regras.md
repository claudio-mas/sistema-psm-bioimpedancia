# Anexo — fonte das regras VBA

Transcrição documental de rotinas extraídas do XLSM atual. Não é implementação web. Números à esquerda são linhas na extração UTF-8, com quebras normalizadas. Comentários e linhas vazias foram omitidos dentro das rotinas; instruções foram preservadas.

SHA-256 do XLSM: `c1f34adaec04a8303c15970c376af7b400490e5fa51d54f317557bbc564ac773`. A existência de uma função não comprova que um fluxo a chama. Ver catálogo e divergências.

## Idade.bas — Age

Origem: `Idade.bas:2`.

```text
2: Function Age(varBirthDate As Variant) As Integer
3:  Dim varAge As Variant
5:  If IsNull(varBirthDate) Then Age = 0: Exit Function
7:  varAge = DateDiff("yyyy", varBirthDate, Now)
8:  If Date < DateSerial(Year(Now), Month(varBirthDate), _
9:  Day(varBirthDate)) Then
10:  varAge = varAge - 1
11:  End If
12:  Age = CInt(varAge)
13: End Function
```

## Energia.bas — tmbOMS

Origem: `Energia.bas:4`.

```text
4: Public Function tmbOMS(Sexo As String, PesoKg As Double, IdadeAnos As Double) As Double
5: On Error Resume Next
7: If Sexo = "Feminino" Then
9: If IdadeAnos < 3 Then
10: tmbOMS = Round((61 * CDbl(PesoKg)) - 51, 2)
11: ElseIf IdadeAnos < 10 Then
12: tmbOMS = Round((22.5 * CDbl(PesoKg)) + 499, 2)
13: ElseIf IdadeAnos < 18 Then
14: tmbOMS = Round((12.2 * CDbl(PesoKg)) + 746, 2)
15: ElseIf IdadeAnos < 30 Then
16: tmbOMS = Round((14.7 * CDbl(PesoKg)) + 496, 2)
17: ElseIf IdadeAnos < 60 Then
18: tmbOMS = Round((8.7 * CDbl(PesoKg)) + 829, 2)
19: ElseIf IdadeAnos >= 60 Then
20: tmbOMS = Round((10.5 * CDbl(PesoKg)) + 596, 2)
21: End If
23: Else
24: If IdadeAnos < 3 Then
25: tmbOMS = Round((60.9 * CDbl(PesoKg)) - 54, 2)
26: ElseIf IdadeAnos < 10 Then
27: tmbOMS = Round((22.7 * CDbl(PesoKg)) + 495, 2)
28: ElseIf IdadeAnos < 18 Then
29: tmbOMS = Round((17.5 * CDbl(PesoKg)) + 651, 2)
30: ElseIf IdadeAnos < 30 Then
31: tmbOMS = Round((15.3 * CDbl(PesoKg)) + 679, 2)
32: ElseIf IdadeAnos < 60 Then
33: tmbOMS = Round((11.6 * CDbl(PesoKg)) + 879, 2)
34: ElseIf IdadeAnos >= 60 Then
35: tmbOMS = Round((13.5 * CDbl(PesoKg)) + 487, 2)
36: End If
38: End If
39: End Function
```

## Energia.bas — Harris_Benedict

Origem: `Energia.bas:41`.

```text
41: Public Function Harris_Benedict(Sexo As String, PesoKg As Double, AlturaCm As Double, IdadeAnos As Integer) As Double
42: On Error Resume Next
45: If Sexo = "Feminino" Then
48: Harris_Benedict = Round(655.1 + (9.563 * CDbl(PesoKg)) + (1.85 * CDbl(AlturaCm)) - (4.676 * CInt(IdadeAnos)))
50: Else
53: Harris_Benedict = Round(66.5 + (13.75 * CDbl(PesoKg)) + (5.003 * CDbl(AlturaCm)) - (6.755 * CInt(IdadeAnos)))
55: End If
57: End Function
```

## Energia.bas — DRIS_Adolescentes

Origem: `Energia.bas:59`.

```text
59: Public Function DRIS_Adolescentes(Sexo As String, PesoKg As Double, AlturaCm As Double, IdadeAnos As Integer) As Double
60: On Error Resume Next
63: Dim IMC As Double
64: IMC = CDbl(PesoKg) / (CDbl(AlturaCm) ^ 2)
66: If Sexo = "Feminino" Then
68:  If IMC <= 21.8 Then
70: DRIS_Adolescentes = Round(189 - (17.6 * CInt(IdadeAnos)) + (625 * (CDbl(AlturaCm) / 100)) + (7.9 * CDbl(PesoKg)))
72:   ElseIf IMC > 21.8 Then
75: DRIS_Adolescentes = Round(515.8 - (26.8 * CInt(IdadeAnos)) + (347 * (CDbl(AlturaCm) / 100)) + (12.4 * CDbl(PesoKg)))
77: End If
79: Else
80: If IMC <= 21.8 Then
83: DRIS_Adolescentes = Round(68 - (43.3 * CInt(IdadeAnos)) + (712 * (CDbl(AlturaCm) / 100)) + (19.2 * CDbl(PesoKg)))
85: ElseIf IMC > 21.8 Then
88: DRIS_Adolescentes = Round(419.9 - (35.5 * CInt(IdadeAnos)) + (418.9 * (CDbl(AlturaCm) / 100)) + (16.7 * CDbl(PesoKg)))
90: End If
91: End If
93: End Function
```

## Energia.bas — DRIS_Adultos

Origem: `Energia.bas:94`.

```text
94: Public Function DRIS_Adultos(Sexo As String, PesoKg As Double, AlturaCm As Double, IdadeAnos As Integer) As Double
95: On Error Resume Next
98: Dim IMC As Double
99: IMC = CDbl(PesoKg) / (CDbl(AlturaCm) ^ 2)
101: If Sexo = "Feminino" Then
103:  If IMC <= 24.99 Then
105: DRIS_Adultos = Round(255 - (2.35 * CInt(IdadeAnos)) + (361.6 * (CDbl(AlturaCm) / 100)) + (9.39 * CDbl(PesoKg)))
107: ElseIf IMC > 24.99 Then
110: DRIS_Adultos = Round(247 - (2.67 * CInt(IdadeAnos)) + (401.5 * (CDbl(AlturaCm) / 100)) + (8.6 * CDbl(PesoKg)))
113: End If
115: Else
116: If IMC <= 24.99 Then
119: DRIS_Adultos = Round(204 - (4 * CInt(IdadeAnos)) + (450.5 * (CDbl(AlturaCm) / 100)) + (11.69 * CDbl(PesoKg)))
121: ElseIf IMC > 24.99 Then
124: DRIS_Adultos = Round(293 - (3.8 * CInt(IdadeAnos)) + (456.4 * (CDbl(AlturaCm) / 100)) + (10.12 * CDbl(PesoKg)))
127: End If
128: End If
130: End Function
```

## IMC.bas — IMCidoso

Origem: `IMC.bas:4`.

```text
4: Public Function IMCidoso(IMC_ As Double) As String
10:             If IMC_ < (22) Then
11:             IMCidoso = "Desnutrição"
12:             Exit Function
13:             ElseIf IMC_ <= (27) Then
14:             IMCidoso = "Eutrofia - Peso Normal"
15:             Exit Function
16:             ElseIf IMC_ > (27) Then
17:             IMCidoso = "Obesidade"
18:             Exit Function
20:             End If
21: End Function
```

## IMC.bas — TabelaIMC

Origem: `IMC.bas:22`.

```text
22: Public Function TabelaIMC(IMC_ As Double) As String
24:             If IMC_ < (18.5) Then
25:             TabelaIMC = "Abaixo do Peso"
26:             Exit Function
27:             ElseIf IMC_ < (25) Then
28:             TabelaIMC = "Peso Normal"
29:             Exit Function
30:             ElseIf IMC_ < (30) Then
31:             TabelaIMC = "Sobrepeso"
32:             Exit Function
33:             ElseIf IMC_ < (35) Then
34:             TabelaIMC = "Obesidade Classe I"
35:             Exit Function
36:             ElseIf IMC_ < (40) Then
37:             TabelaIMC = "Obesidade Classe II"
38:             Exit Function
39:             ElseIf IMC_ >= (40) And IMC_ Then
40:             TabelaIMC = "Obesidade Classe III"
41:             Exit Function
42:          End If
43: End Function
```

## IMC.bas — Imc_CA_CAM

Origem: `IMC.bas:47`.

```text
47: Public Function Imc_CA_CAM(sexo_ As String, Idade_ As Integer, IMC As Double) As Variant
49: If sexo_ = "Feminino" Then
50:     If Idade_ = (5) Then
51:             If IMC < (12.41) Then
52:             Imc_CA_CAM = 0.1
53:             ImcDesc = "Magreza acentuada"
54:             Exit Function
55:             End If
57:             If IMC <= (12.87) Then
58:             Imc_CA_CAM = 3
59:             ImcDesc = "Magreza"
60:             Exit Function
61:             End If
63:             If IMC <= (13.13) Then
64:             Imc_CA_CAM = 5
65:             ImcDesc = "Eutrofia (Normal)"
66:             Exit Function
68:             ElseIf IMC <= (13.55) Then
69:             Imc_CA_CAM = 10
70:             ImcDesc = "Eutrofia (Normal)"
71:             Exit Function
73:             ElseIf IMC <= (13.85) Then
74:             Imc_CA_CAM = 15
75:             ImcDesc = "Eutrofia (Normal)"
76:             Exit Function
78:             ElseIf IMC <= (14.31) Then
79:             Imc_CA_CAM = 25
80:             ImcDesc = "Eutrofia (Normal)"
81:             Exit Function
83:             ElseIf IMC <= (15.24) Then
84:             Imc_CA_CAM = 50
85:             ImcDesc = "Eutrofia (Normal)"
86:             Exit Function
88:             ElseIf IMC <= (16.31) Then
89:             Imc_CA_CAM = 75
90:             ImcDesc = "Eutrofia (Normal)"
91:             Exit Function
93:             ElseIf IMC <= (16.94) Then
94:             Imc_CA_CAM = 85
95:             ImcDesc = "Eutrofia (Normal)"
96:             Exit Function
98:             End If
100:             If IMC <= (17.39) Then
101:             Imc_CA_CAM = 90
102:             ImcDesc = "Sobrepeso"
103:             Exit Function
105:             ElseIf IMC <= (18.1) Then
106:             Imc_CA_CAM = 95
107:             ImcDesc = "Sobrepeso"
108:             Exit Function
110:             ElseIf IMC < (18.6) Then
111:             Imc_CA_CAM = 97
112:             ImcDesc = "Sobrepeso"
113:             Exit Function
115:             End If
117:             If IMC <= (21.59) Then
118:             Imc_CA_CAM = 99
119:             ImcDesc = "Obesidade_"
120:             Exit Function
121:             End If
123:             If IMC > (21.59) Then
124:             Imc_CA_CAM = 99.9
125:             ImcDesc = "Obesidade_ grave"
126:             Exit Function
127:             End If
130:        End If
133:        If Idade_ = (6) Then
134:             If IMC < (12.36) Then
135:             Imc_CA_CAM = 0.1
136:             ImcDesc = "Magreza acentuada"
137:             Exit Function
138:             End If
140:             If IMC <= (12.83) Then
141:             Imc_CA_CAM = 3
142:             ImcDesc = "Magreza"
143:             Exit Function
144:             End If
146:             If IMC <= (13.09) Then
147:             Imc_CA_CAM = 5
148:             ImcDesc = "Eutrofia (Normal)"
149:             Exit Function
151:             ElseIf IMC <= (13.51) Then
152:             Imc_CA_CAM = 10
153:             ImcDesc = "Eutrofia (Normal)"
154:             Exit Function
156:             ElseIf IMC <= (13.82) Then
157:             Imc_CA_CAM = 15
158:             ImcDesc = "Eutrofia (Normal)"
159:             Exit Function
161:             ElseIf IMC <= (14.29) Then
162:             Imc_CA_CAM = 25
163:             ImcDesc = "Eutrofia (Normal)"
164:             Exit Function
166:             ElseIf IMC <= (15.27) Then
167:             Imc_CA_CAM = 50
168:             ImcDesc = "Eutrofia (Normal)"
169:             Exit Function
171:             ElseIf IMC <= (16.4) Then
172:             Imc_CA_CAM = 75
173:             ImcDesc = "Eutrofia (Normal)"
174:             Exit Function
176:             ElseIf IMC <= (17.08) Then
177:             Imc_CA_CAM = 85
178:             ImcDesc = "Eutrofia (Normal)"
179:             Exit Function
181:             End If
183:             If IMC <= (17.58) Then
184:             Imc_CA_CAM = 90
185:             ImcDesc = "Sobrepeso"
186:             Exit Function
188:             ElseIf IMC <= (18.37) Then
189:             Imc_CA_CAM = 95
190:             ImcDesc = "Sobrepeso"
191:             Exit Function
193:             ElseIf IMC < (18.93) Then
194:             Imc_CA_CAM = 97
195:             ImcDesc = "Sobrepeso"
196:             Exit Function
198:             End If
200:             If IMC <= (22.44) Then
201:             Imc_CA_CAM = 99
202:             ImcDesc = "Obesidade_"
203:             Exit Function
204:             End If
206:             If IMC > (22.44) Then
207:             Imc_CA_CAM = 99.9
208:             ImcDesc = "Obesidade_ grave"
209:             Exit Function
210:             End If
213:        End If
215:       If Idade_ = (7) Then
216:             If IMC < (12.39) Then
217:             Imc_CA_CAM = 0.1
218:             ImcDesc = "Magreza acentuada"
219:             Exit Function
220:             End If
222:             If IMC <= (12.87) Then
223:             Imc_CA_CAM = 3
224:             ImcDesc = "Magreza"
225:             Exit Function
226:             End If
228:             If IMC <= (13.13) Then
229:             Imc_CA_CAM = 5
230:             ImcDesc = "Eutrofia (Normal)"
231:             Exit Function
233:             ElseIf IMC <= (13.57) Then
234:             Imc_CA_CAM = 10
235:             ImcDesc = "Eutrofia (Normal)"
236:             Exit Function
238:             ElseIf IMC <= (13.88) Then
239:             Imc_CA_CAM = 15
240:             ImcDesc = "Eutrofia (Normal)"
241:             Exit Function
243:             ElseIf IMC <= (14.37) Then
244:             Imc_CA_CAM = 25
245:             ImcDesc = "Eutrofia (Normal)"
246:             Exit Function
248:             ElseIf IMC <= (15.4) Then
249:             Imc_CA_CAM = 50
250:             ImcDesc = "Eutrofia (Normal)"
251:             Exit Function
253:             ElseIf IMC <= (16.62) Then
254:             Imc_CA_CAM = 75
255:             ImcDesc = "Eutrofia (Normal)"
256:             Exit Function
258:             ElseIf IMC <= (17.37) Then
259:             Imc_CA_CAM = 85
260:             ImcDesc = "Eutrofia (Normal)"
261:             Exit Function
263:             End If
265:             If IMC <= (17.92) Then
266:             Imc_CA_CAM = 90
267:             ImcDesc = "Sobrepeso"
268:             Exit Function
270:             ElseIf IMC <= (18.81) Then
271:             Imc_CA_CAM = 95
272:             ImcDesc = "Sobrepeso"
273:             Exit Function
275:             ElseIf IMC < (19.45) Then
276:             Imc_CA_CAM = 97
277:             ImcDesc = "Sobrepeso"
278:             Exit Function
280:             End If
282:             If IMC <= (23.67) Then
283:             Imc_CA_CAM = 99
284:             ImcDesc = "Obesidade_"
285:             Exit Function
286:             End If
288:             If IMC > (23.67) Then
289:             Imc_CA_CAM = 99.9
290:             ImcDesc = "Obesidade_ grave"
291:             Exit Function
292:             End If
295:        End If
298:             If Idade_ = (8) Then
299:             If IMC < (12.54) Then
300:             Imc_CA_CAM = 0.1
301:             ImcDesc = "Magreza acentuada"
302:             Exit Function
303:             End If
305:             If IMC <= (13.02) Then
306:             Imc_CA_CAM = 3
307:             ImcDesc = "Magreza"
308:             Exit Function
309:             End If
311:             If IMC <= (13.29) Then
312:             Imc_CA_CAM = 5
313:             ImcDesc = "Eutrofia (Normal)"
314:             Exit Function
316:             ElseIf IMC <= (13.74) Then
317:             Imc_CA_CAM = 10
318:             ImcDesc = "Eutrofia (Normal)"
319:             Exit Function
321:             ElseIf IMC <= (14.07) Then
322:             Imc_CA_CAM = 15
323:             ImcDesc = "Eutrofia (Normal)"
324:             Exit Function
326:             ElseIf IMC <= (14.59) Then
327:             Imc_CA_CAM = 25
328:             ImcDesc = "Eutrofia (Normal)"
329:             Exit Function
331:             ElseIf IMC <= (15.68) Then
332:             Imc_CA_CAM = 50
333:             ImcDesc = "Eutrofia (Normal)"
334:             Exit Function
336:             ElseIf IMC <= (17) Then
337:             Imc_CA_CAM = 75
338:             ImcDesc = "Eutrofia (Normal)"
339:             Exit Function
341:             ElseIf IMC <= (17.82) Then
342:             Imc_CA_CAM = 85
343:             ImcDesc = "Eutrofia (Normal)"
344:             Exit Function
346:             End If
348:             If IMC <= (18.43) Then
349:             Imc_CA_CAM = 90
350:             ImcDesc = "Sobrepeso"
351:             Exit Function
353:             ElseIf IMC <= (19.44) Then
354:             Imc_CA_CAM = 95
355:             ImcDesc = "Sobrepeso"
356:             Exit Function
358:             ElseIf IMC < (20.17) Then
359:             Imc_CA_CAM = 97
360:             ImcDesc = "Sobrepeso"
361:             Exit Function
363:             End If
365:             If IMC <= (25.27) Then
366:             Imc_CA_CAM = 99
367:             ImcDesc = "Obesidade_"
368:             Exit Function
369:             End If
371:             If IMC > (25.27) Then
372:             Imc_CA_CAM = 99.9
373:             ImcDesc = "Obesidade_ grave"
374:             Exit Function
375:             End If
378:        End If
380:        If Idade_ = (9) Then
381:             If IMC < (12.78) Then
382:             Imc_CA_CAM = 0.1
383:             ImcDesc = "Magreza acentuada"
384:             Exit Function
385:             End If
387:             If IMC <= (13.28) Then
388:             Imc_CA_CAM = 3
389:             ImcDesc = "Magreza"
390:             Exit Function
391:             End If
393:             If IMC <= (13.57) Then
394:             Imc_CA_CAM = 5
395:             ImcDesc = "Eutrofia (Normal)"
396:             Exit Function
398:             ElseIf IMC <= (14.04) Then
399:             Imc_CA_CAM = 10
400:             ImcDesc = "Eutrofia (Normal)"
401:             Exit Function
403:             ElseIf IMC <= (14.38) Then
404:             Imc_CA_CAM = 15
405:             ImcDesc = "Eutrofia (Normal)"
406:             Exit Function
408:             ElseIf IMC <= (14.93) Then
409:             Imc_CA_CAM = 25
410:             ImcDesc = "Eutrofia (Normal)"
411:             Exit Function
413:             ElseIf IMC <= (16.1) Then
414:             Imc_CA_CAM = 50
415:             ImcDesc = "Eutrofia (Normal)"
416:             Exit Function
418:             ElseIf IMC <= (17.52) Then
419:             Imc_CA_CAM = 75
420:             ImcDesc = "Eutrofia (Normal)"
421:             Exit Function
423:             ElseIf IMC <= (18.42) Then
424:             Imc_CA_CAM = 85
425:             ImcDesc = "Eutrofia (Normal)"
426:             Exit Function
428:             End If
430:             If IMC <= (19.1) Then
431:             Imc_CA_CAM = 90
432:             ImcDesc = "Sobrepeso"
433:             Exit Function
435:             ElseIf IMC <= (20.23) Then
436:             Imc_CA_CAM = 95
437:             ImcDesc = "Sobrepeso"
438:             Exit Function
440:             ElseIf IMC < (21.06) Then
441:             Imc_CA_CAM = 97
442:             ImcDesc = "Sobrepeso"
443:             Exit Function
445:             End If
447:             If IMC <= (27.14) Then
448:             Imc_CA_CAM = 99
449:             ImcDesc = "Obesidade_"
450:             Exit Function
451:             End If
453:             If IMC > (27.14) Then
454:             Imc_CA_CAM = 99.9
455:             ImcDesc = "Obesidade_ grave"
456:             Exit Function
457:             End If
460:        End If
462:              If Idade_ = (10) Then
463:             If IMC < (13.09) Then
464:             Imc_CA_CAM = 0.1
465:             ImcDesc = "Magreza acentuada"
466:             Exit Function
467:             End If
469:             If IMC <= (13.62) Then
470:             Imc_CA_CAM = 3
471:             ImcDesc = "Magreza"
472:             Exit Function
473:             End If
475:             If IMC <= (13.92) Then
476:             Imc_CA_CAM = 5
477:             ImcDesc = "Eutrofia (Normal)"
478:             Exit Function
480:             ElseIf IMC <= (14.42) Then
481:             Imc_CA_CAM = 10
482:             ImcDesc = "Eutrofia (Normal)"
483:             Exit Function
485:             ElseIf IMC <= (14.78) Then
486:             Imc_CA_CAM = 15
487:             ImcDesc = "Eutrofia (Normal)"
488:             Exit Function
490:             ElseIf IMC <= (15.36) Then
491:             Imc_CA_CAM = 25
492:             ImcDesc = "Eutrofia (Normal)"
493:             Exit Function
495:             ElseIf IMC <= (16.61) Then
496:             Imc_CA_CAM = 50
497:             ImcDesc = "Eutrofia (Normal)"
498:             Exit Function
500:             ElseIf IMC <= (18.15) Then
501:             Imc_CA_CAM = 75
502:             ImcDesc = "Eutrofia (Normal)"
503:             Exit Function
505:             ElseIf IMC <= (19.14) Then
506:             Imc_CA_CAM = 85
507:             ImcDesc = "Eutrofia (Normal)"
508:             Exit Function
510:             End If
512:             If IMC <= (19.88) Then
513:             Imc_CA_CAM = 90
514:             ImcDesc = "Sobrepeso"
515:             Exit Function
517:             ElseIf IMC <= (21.14) Then
518:             Imc_CA_CAM = 95
519:             ImcDesc = "Sobrepeso"
520:             Exit Function
522:             ElseIf IMC < (22.06) Then
523:             Imc_CA_CAM = 97
524:             ImcDesc = "Sobrepeso"
525:             Exit Function
527:             End If
529:             If IMC <= (29.1) Then
530:             Imc_CA_CAM = 99
531:             ImcDesc = "Obesidade_"
532:             Exit Function
533:             End If
535:             If IMC > (29.1) Then
536:             Imc_CA_CAM = 99.9
537:             ImcDesc = "Obesidade_ grave"
538:             Exit Function
539:             End If
542:        End If
544:                     If Idade_ = (11) Then
545:             If IMC < (13.48) Then
546:             Imc_CA_CAM = 0.1
547:             ImcDesc = "Magreza acentuada"
548:             Exit Function
549:             End If
551:             If IMC <= (14.04) Then
552:             Imc_CA_CAM = 3
553:             ImcDesc = "Magreza"
554:             Exit Function
555:             End If
557:             If IMC <= (14.36) Then
558:             Imc_CA_CAM = 5
559:             ImcDesc = "Eutrofia (Normal)"
560:             Exit Function
562:             ElseIf IMC <= (14.9) Then
563:             Imc_CA_CAM = 10
564:             ImcDesc = "Eutrofia (Normal)"
565:             Exit Function
567:             ElseIf IMC <= (15.28) Then
568:             Imc_CA_CAM = 15
569:             ImcDesc = "Eutrofia (Normal)"
570:             Exit Function
572:             ElseIf IMC <= (15.9) Then
573:             Imc_CA_CAM = 25
574:             ImcDesc = "Eutrofia (Normal)"
575:             Exit Function
577:             ElseIf IMC <= (17.25) Then
578:             Imc_CA_CAM = 50
579:             ImcDesc = "Eutrofia (Normal)"
580:             Exit Function
582:             ElseIf IMC <= (18.91) Then
583:             Imc_CA_CAM = 75
584:             ImcDesc = "Eutrofia (Normal)"
585:             Exit Function
587:             ElseIf IMC <= (19.97) Then
588:             Imc_CA_CAM = 85
589:             ImcDesc = "Eutrofia (Normal)"
590:             Exit Function
592:             End If
594:             If IMC <= (20.79) Then
595:             Imc_CA_CAM = 90
596:             ImcDesc = "Sobrepeso"
597:             Exit Function
599:             ElseIf IMC <= (22.15) Then
600:             Imc_CA_CAM = 95
601:             ImcDesc = "Sobrepeso"
602:             Exit Function
604:             ElseIf IMC < (23.17) Then
605:             Imc_CA_CAM = 97
606:             ImcDesc = "Sobrepeso"
607:             Exit Function
609:             End If
611:             If IMC <= (31) Then
612:             Imc_CA_CAM = 99
613:             ImcDesc = "Obesidade_"
614:             Exit Function
615:             End If
617:             If IMC > (31) Then
618:             Imc_CA_CAM = 99.9
619:             ImcDesc = "Obesidade_ grave"
620:             Exit Function
621:             End If
624:        End If
626:                     If Idade_ = (12) Then
627:             If IMC < (13.96) Then
628:             Imc_CA_CAM = 0.1
629:             ImcDesc = "Magreza acentuada"
630:             Exit Function
631:             End If
633:             If IMC <= (14.56) Then
634:             Imc_CA_CAM = 3
635:             ImcDesc = "Magreza"
636:             Exit Function
637:             End If
639:             If IMC <= (14.9) Then
640:             Imc_CA_CAM = 5
641:             ImcDesc = "Eutrofia (Normal)"
642:             Exit Function
644:             ElseIf IMC <= (15.47) Then
645:             Imc_CA_CAM = 10
646:             ImcDesc = "Eutrofia (Normal)"
647:             Exit Function
649:             ElseIf IMC <= (15.89) Then
650:             Imc_CA_CAM = 15
651:             ImcDesc = "Eutrofia (Normal)"
652:             Exit Function
654:             ElseIf IMC <= (16.56) Then
655:             Imc_CA_CAM = 25
656:             ImcDesc = "Eutrofia (Normal)"
657:             Exit Function
659:             ElseIf IMC <= (18) Then
660:             Imc_CA_CAM = 50
661:             ImcDesc = "Eutrofia (Normal)"
662:             Exit Function
664:             ElseIf IMC <= (19.78) Then
665:             Imc_CA_CAM = 75
666:             ImcDesc = "Eutrofia (Normal)"
667:             Exit Function
669:             ElseIf IMC <= (20.93) Then
670:             Imc_CA_CAM = 85
671:             ImcDesc = "Eutrofia (Normal)"
672:             Exit Function
674:             End If
676:             If IMC <= (21.8) Then
677:             Imc_CA_CAM = 90
678:             ImcDesc = "Sobrepeso"
679:             Exit Function
681:             ElseIf IMC <= (23.28) Then
682:             Imc_CA_CAM = 95
683:             ImcDesc = "Sobrepeso"
684:             Exit Function
686:             ElseIf IMC < (24.37) Then
687:             Imc_CA_CAM = 97
688:             ImcDesc = "Sobrepeso"
689:             Exit Function
691:             End If
693:             If IMC <= (32.78) Then
694:             Imc_CA_CAM = 99
695:             ImcDesc = "Obesidade_"
696:             Exit Function
697:             End If
699:             If IMC > (32.78) Then
700:             Imc_CA_CAM = 99.9
701:             ImcDesc = "Obesidade_ grave"
702:             Exit Function
703:             End If
706:        End If
708:                     If Idade_ = (13) Then
709:             If IMC < (14.47) Then
710:             Imc_CA_CAM = 0.1
711:             ImcDesc = "Magreza acentuada"
712:             Exit Function
713:             End If
715:             If IMC <= (15.12) Then
716:             Imc_CA_CAM = 3
717:             ImcDesc = "Magreza"
718:             Exit Function
719:             End If
721:             If IMC <= (15.49) Then
722:             Imc_CA_CAM = 5
723:             ImcDesc = "Eutrofia (Normal)"
724:             Exit Function
726:             ElseIf IMC <= (16.1) Then
727:             Imc_CA_CAM = 10
728:             ImcDesc = "Eutrofia (Normal)"
729:             Exit Function
731:             ElseIf IMC <= (16.54) Then
732:             Imc_CA_CAM = 15
733:             ImcDesc = "Eutrofia (Normal)"
734:             Exit Function
736:             ElseIf IMC <= (17.26) Then
737:             Imc_CA_CAM = 25
738:             ImcDesc = "Eutrofia (Normal)"
739:             Exit Function
741:             ElseIf IMC <= (18.8) Then
742:             Imc_CA_CAM = 50
743:             ImcDesc = "Eutrofia (Normal)"
744:             Exit Function
746:             ElseIf IMC <= (20.71) Then
747:             Imc_CA_CAM = 75
748:             ImcDesc = "Eutrofia (Normal)"
749:             Exit Function
751:             ElseIf IMC <= (21.93) Then
752:             Imc_CA_CAM = 85
753:             ImcDesc = "Eutrofia (Normal)"
754:             Exit Function
756:             End If
758:             If IMC <= (22.86) Then
759:             Imc_CA_CAM = 90
760:             ImcDesc = "Sobrepeso"
761:             Exit Function
763:             ElseIf IMC <= (24.42) Then
764:             Imc_CA_CAM = 95
765:             ImcDesc = "Sobrepeso"
766:             Exit Function
768:             ElseIf IMC < (25.57) Then
769:             Imc_CA_CAM = 97
770:             ImcDesc = "Sobrepeso"
771:             Exit Function
773:             End If
775:             If IMC <= (34.33) Then
776:             Imc_CA_CAM = 99
777:             ImcDesc = "Obesidade_"
778:             Exit Function
779:             End If
781:             If IMC > (34.33) Then
782:             Imc_CA_CAM = 99.9
783:             ImcDesc = "Obesidade_ grave"
784:             Exit Function
785:             End If
788:        End If
790:                     If Idade_ = (14) Then
791:             If IMC < (14.95) Then
792:             Imc_CA_CAM = 0.1
793:             ImcDesc = "Magreza acentuada"
794:             Exit Function
795:             End If
797:             If IMC <= (15.64) Then
798:             Imc_CA_CAM = 3
799:             ImcDesc = "Magreza"
800:             Exit Function
801:             End If
803:             If IMC <= (16.04) Then
804:             Imc_CA_CAM = 5
805:             ImcDesc = "Eutrofia (Normal)"
806:             Exit Function
808:             ElseIf IMC <= (16.69) Then
809:             Imc_CA_CAM = 10
810:             ImcDesc = "Eutrofia (Normal)"
811:             Exit Function
813:             ElseIf IMC <= (17.16) Then
814:             Imc_CA_CAM = 15
815:             ImcDesc = "Eutrofia (Normal)"
816:             Exit Function
818:             ElseIf IMC <= (17.93) Then
819:             Imc_CA_CAM = 25
820:             ImcDesc = "Eutrofia (Normal)"
821:             Exit Function
823:             ElseIf IMC <= (19.57) Then
824:             Imc_CA_CAM = 50
825:             ImcDesc = "Eutrofia (Normal)"
826:             Exit Function
828:             ElseIf IMC <= (21.58) Then
829:             Imc_CA_CAM = 75
830:             ImcDesc = "Eutrofia (Normal)"
831:             Exit Function
833:             ElseIf IMC <= (22.87) Then
834:             Imc_CA_CAM = 85
835:             ImcDesc = "Eutrofia (Normal)"
836:             Exit Function
838:             End If
840:             If IMC <= (23.84) Then
841:             Imc_CA_CAM = 90
842:             ImcDesc = "Sobrepeso"
843:             Exit Function
845:             ElseIf IMC <= (25.47) Then
846:             Imc_CA_CAM = 95
847:             ImcDesc = "Sobrepeso"
848:             Exit Function
850:             ElseIf IMC < (26.67) Then
851:             Imc_CA_CAM = 97
852:             ImcDesc = "Sobrepeso"
853:             Exit Function
855:             End If
857:             If IMC <= (35.55) Then
858:             Imc_CA_CAM = 99
859:             ImcDesc = "Obesidade_"
860:             Exit Function
861:             End If
863:             If IMC > (35.55) Then
864:             Imc_CA_CAM = 99.9
865:             ImcDesc = "Obesidade_ grave"
866:             Exit Function
867:             End If
870:        End If
872:                     If Idade_ = (15) Then
873:             If IMC < (15.34) Then
874:             Imc_CA_CAM = 0.1
875:             ImcDesc = "Magreza acentuada"
876:             Exit Function
877:             End If
879:             If IMC <= (16.07) Then
880:             Imc_CA_CAM = 3
881:             ImcDesc = "Magreza"
882:             Exit Function
883:             End If
885:             If IMC <= (16.49) Then
886:             Imc_CA_CAM = 5
887:             ImcDesc = "Eutrofia (Normal)"
888:             Exit Function
890:             ElseIf IMC <= (17.18) Then
891:             Imc_CA_CAM = 10
892:             ImcDesc = "Eutrofia (Normal)"
893:             Exit Function
895:             ElseIf IMC <= (17.69) Then
896:             Imc_CA_CAM = 15
897:             ImcDesc = "Eutrofia (Normal)"
898:             Exit Function
900:             ElseIf IMC <= (18.49) Then
901:             Imc_CA_CAM = 25
902:             ImcDesc = "Eutrofia (Normal)"
903:             Exit Function
905:             ElseIf IMC <= (20.21) Then
906:             Imc_CA_CAM = 50
907:             ImcDesc = "Eutrofia (Normal)"
908:             Exit Function
910:             ElseIf IMC <= (22.32) Then
911:             Imc_CA_CAM = 75
912:             ImcDesc = "Eutrofia (Normal)"
913:             Exit Function
915:             ElseIf IMC <= (23.66) Then
916:             Imc_CA_CAM = 85
917:             ImcDesc = "Eutrofia (Normal)"
918:             Exit Function
920:             End If
922:             If IMC <= (24.66) Then
923:             Imc_CA_CAM = 90
924:             ImcDesc = "Sobrepeso"
925:             Exit Function
927:             ElseIf IMC <= (26.34) Then
928:             Imc_CA_CAM = 95
929:             ImcDesc = "Sobrepeso"
930:             Exit Function
932:             ElseIf IMC < (27.56) Then
933:             Imc_CA_CAM = 97
934:             ImcDesc = "Sobrepeso"
935:             Exit Function
937:             End If
939:             If IMC <= (36.4) Then
940:             Imc_CA_CAM = 99
941:             ImcDesc = "Obesidade_"
942:             Exit Function
943:             End If
945:             If IMC > (36.4) Then
946:             Imc_CA_CAM = 99.9
947:             ImcDesc = "Obesidade_ grave"
948:             Exit Function
949:             End If
952:        End If
954:                     If Idade_ = (16) Then
955:             If IMC < (15.62) Then
956:             Imc_CA_CAM = 0.1
957:             ImcDesc = "Magreza acentuada"
958:             Exit Function
959:             End If
961:             If IMC <= (16.38) Then
962:             Imc_CA_CAM = 3
963:             ImcDesc = "Magreza"
964:             Exit Function
965:             End If
967:             If IMC <= (16.82) Then
968:             Imc_CA_CAM = 5
969:             ImcDesc = "Eutrofia (Normal)"
970:             Exit Function
972:             ElseIf IMC <= (17.55) Then
973:             Imc_CA_CAM = 10
974:             ImcDesc = "Eutrofia (Normal)"
975:             Exit Function
977:             ElseIf IMC <= (18.07) Then
978:             Imc_CA_CAM = 15
979:             ImcDesc = "Eutrofia (Normal)"
980:             Exit Function
982:             ElseIf IMC <= (18.91) Then
983:             Imc_CA_CAM = 25
984:             ImcDesc = "Eutrofia (Normal)"
985:             Exit Function
987:             ElseIf IMC <= (20.7) Then
988:             Imc_CA_CAM = 50
989:             ImcDesc = "Eutrofia (Normal)"
990:             Exit Function
992:             ElseIf IMC <= (22.88) Then
993:             Imc_CA_CAM = 75
994:             ImcDesc = "Eutrofia (Normal)"
995:             Exit Function
997:             ElseIf IMC <= (24.25) Then
998:             Imc_CA_CAM = 85
999:             ImcDesc = "Eutrofia (Normal)"
1000:             Exit Function
1002:             End If
1004:             If IMC <= (25.27) Then
1005:             Imc_CA_CAM = 90
1006:             ImcDesc = "Sobrepeso"
1007:             Exit Function
1009:             ElseIf IMC <= (26.97) Then
1010:             Imc_CA_CAM = 95
1011:             ImcDesc = "Sobrepeso"
1012:             Exit Function
1014:             ElseIf IMC < (28.21) Then
1015:             Imc_CA_CAM = 97
1016:             ImcDesc = "Sobrepeso"
1017:             Exit Function
1019:             End If
1021:             If IMC <= (36.9) Then
1022:             Imc_CA_CAM = 99
1023:             ImcDesc = "Obesidade_"
1024:             Exit Function
1025:             End If
1027:             If IMC > (36.9) Then
1028:             Imc_CA_CAM = 99.9
1029:             ImcDesc = "Obesidade_ grave"
1030:             Exit Function
1031:             End If
1034:        End If
1036:                     If Idade_ = (17) Then
1037:             If IMC < (15.78) Then
1038:             Imc_CA_CAM = 0.1
1039:             ImcDesc = "Magreza acentuada"
1040:             Exit Function
1041:             End If
1043:             If IMC <= (16.58) Then
1044:             Imc_CA_CAM = 3
1045:             ImcDesc = "Magreza"
1046:             Exit Function
1047:             End If
1049:             If IMC <= (17.03) Then
1050:             Imc_CA_CAM = 5
1051:             ImcDesc = "Eutrofia (Normal)"
1052:             Exit Function
1054:             ElseIf IMC <= (17.78) Then
1055:             Imc_CA_CAM = 10
1056:             ImcDesc = "Eutrofia (Normal)"
1057:             Exit Function
1059:             ElseIf IMC <= (18.33) Then
1060:             Imc_CA_CAM = 15
1061:             ImcDesc = "Eutrofia (Normal)"
1062:             Exit Function
1064:             ElseIf IMC <= (19.19) Then
1065:             Imc_CA_CAM = 25
1066:             ImcDesc = "Eutrofia (Normal)"
1067:             Exit Function
1069:             ElseIf IMC <= (21.04) Then
1070:             Imc_CA_CAM = 50
1071:             ImcDesc = "Eutrofia (Normal)"
1072:             Exit Function
1074:             ElseIf IMC <= (23.26) Then
1075:             Imc_CA_CAM = 75
1076:             ImcDesc = "Eutrofia (Normal)"
1077:             Exit Function
1079:             ElseIf IMC <= (24.65) Then
1080:             Imc_CA_CAM = 85
1081:             ImcDesc = "Eutrofia (Normal)"
1082:             Exit Function
1084:             End If
1086:             If IMC <= (25.69) Then
1087:             Imc_CA_CAM = 90
1088:             ImcDesc = "Sobrepeso"
1089:             Exit Function
1091:             ElseIf IMC <= (27.39) Then
1092:             Imc_CA_CAM = 95
1093:             ImcDesc = "Sobrepeso"
1094:             Exit Function
1096:             ElseIf IMC < (28.62) Then
1097:             Imc_CA_CAM = 97
1098:             ImcDesc = "Sobrepeso"
1099:             Exit Function
1101:             End If
1103:             If IMC <= (37.08) Then
1104:             Imc_CA_CAM = 99
1105:             ImcDesc = "Obesidade_"
1106:             Exit Function
1107:             End If
1109:             If IMC > (37.08) Then
1110:             Imc_CA_CAM = 99.9
1111:             ImcDesc = "Obesidade_ grave"
1112:             Exit Function
1113:             End If
1116:        End If
1118:                     If Idade_ = (18) Then
1119:             If IMC < (15.85) Then
1120:             Imc_CA_CAM = 0.1
1121:             ImcDesc = "Magreza acentuada"
1122:             Exit Function
1123:             End If
1125:             If IMC <= (16.68) Then
1126:             Imc_CA_CAM = 3
1127:             ImcDesc = "Magreza"
1128:             Exit Function
1129:             End If
1131:             If IMC <= (17.15) Then
1132:             Imc_CA_CAM = 5
1133:             ImcDesc = "Eutrofia (Normal)"
1134:             Exit Function
1136:             ElseIf IMC <= (17.92) Then
1137:             Imc_CA_CAM = 10
1138:             ImcDesc = "Eutrofia (Normal)"
1139:             Exit Function
1141:             ElseIf IMC <= (18.49) Then
1142:             Imc_CA_CAM = 15
1143:             ImcDesc = "Eutrofia (Normal)"
1144:             Exit Function
1146:             ElseIf IMC <= (19.37) Then
1147:             Imc_CA_CAM = 25
1148:             ImcDesc = "Eutrofia (Normal)"
1149:             Exit Function
1151:             ElseIf IMC <= (21.26) Then
1152:             Imc_CA_CAM = 50
1153:             ImcDesc = "Eutrofia (Normal)"
1154:             Exit Function
1156:             ElseIf IMC <= (23.52) Then
1157:             Imc_CA_CAM = 75
1158:             ImcDesc = "Eutrofia (Normal)"
1159:             Exit Function
1161:             ElseIf IMC <= (24.92) Then
1162:             Imc_CA_CAM = 85
1163:             ImcDesc = "Eutrofia (Normal)"
1164:             Exit Function
1166:             End If
1168:             If IMC <= (25.96) Then
1169:             Imc_CA_CAM = 90
1170:             ImcDesc = "Sobrepeso"
1171:             Exit Function
1173:             ElseIf IMC <= (27.65) Then
1174:             Imc_CA_CAM = 95
1175:             ImcDesc = "Sobrepeso"
1176:             Exit Function
1178:             ElseIf IMC < (28.87) Then
1179:             Imc_CA_CAM = 97
1180:             ImcDesc = "Sobrepeso"
1181:             Exit Function
1183:             End If
1185:             If IMC <= (37.03) Then
1186:             Imc_CA_CAM = 99
1187:             ImcDesc = "Obesidade_"
1188:             Exit Function
1189:             End If
1191:             If IMC > (37.03) Then
1192:             Imc_CA_CAM = 99.9
1193:             ImcDesc = "Obesidade_ grave"
1194:             Exit Function
1195:             End If
1198:        End If
1200:                     If Idade_ = (19) Then
1201:             If IMC < (15.88) Then
1202:             Imc_CA_CAM = 0.1
1203:             ImcDesc = "Magreza acentuada"
1204:             Exit Function
1205:             End If
1207:             If IMC <= (16.73) Then
1208:             Imc_CA_CAM = 3
1209:             ImcDesc = "Magreza"
1210:             Exit Function
1211:             End If
1213:             If IMC <= (17.22) Then
1214:             Imc_CA_CAM = 5
1215:             ImcDesc = "Eutrofia (Normal)"
1216:             Exit Function
1218:             ElseIf IMC <= (18.02) Then
1219:             Imc_CA_CAM = 10
1220:             ImcDesc = "Eutrofia (Normal)"
1221:             Exit Function
1223:             ElseIf IMC <= (18.59) Then
1224:             Imc_CA_CAM = 15
1225:             ImcDesc = "Eutrofia (Normal)"
1226:             Exit Function
1228:             ElseIf IMC <= (19.5) Then
1229:             Imc_CA_CAM = 25
1230:             ImcDesc = "Eutrofia (Normal)"
1231:             Exit Function
1233:             ElseIf IMC <= (21.43) Then
1234:             Imc_CA_CAM = 50
1235:             ImcDesc = "Eutrofia (Normal)"
1236:             Exit Function
1238:             ElseIf IMC <= (23.71) Then
1239:             Imc_CA_CAM = 75
1240:             ImcDesc = "Eutrofia (Normal)"
1241:             Exit Function
1243:             ElseIf IMC <= (25.11) Then
1244:             Imc_CA_CAM = 85
1245:             ImcDesc = "Eutrofia (Normal)"
1246:             Exit Function
1248:             End If
1250:             If IMC <= (26.15) Then
1251:             Imc_CA_CAM = 90
1252:             ImcDesc = "Sobrepeso"
1253:             Exit Function
1255:             ElseIf IMC <= (27.83) Then
1256:             Imc_CA_CAM = 95
1257:             ImcDesc = "Sobrepeso"
1258:             Exit Function
1260:             ElseIf IMC < (29.03) Then
1261:             Imc_CA_CAM = 97
1262:             ImcDesc = "Sobrepeso"
1263:             Exit Function
1265:             End If
1267:             If IMC <= (36.89) Then
1268:             Imc_CA_CAM = 99
1269:             ImcDesc = "Obesidade_"
1270:             Exit Function
1271:             End If
1273:             If IMC > (36.89) Then
1274:             Imc_CA_CAM = 99.9
1275:             ImcDesc = "Obesidade_ grave"
1276:             Exit Function
1277:             End If
1280:        End If
1282:     Else '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
1284:     If Idade_ = (5) Then
1285:             If IMC < (12.72) Then
1286:             Imc_CA_CAM = 0.1
1287:             ImcDesc = "Magreza acentuada"
1288:             Exit Function
1289:             End If
1291:             If IMC <= (13.15) Then
1292:             Imc_CA_CAM = 3
1293:             ImcDesc = "Magreza"
1294:             Exit Function
1295:             End If
1297:             If IMC <= (13.38) Then
1298:             Imc_CA_CAM = 5
1299:             ImcDesc = "Eutrofia (Normal)"
1300:             Exit Function
1302:             ElseIf IMC <= (13.76) Then
1303:             Imc_CA_CAM = 10
1304:             ImcDesc = "Eutrofia (Normal)"
1305:             Exit Function
1307:             ElseIf IMC <= (14.03) Then
1308:             Imc_CA_CAM = 15
1309:             ImcDesc = "Eutrofia (Normal)"
1310:             Exit Function
1312:             ElseIf IMC <= (14.44) Then
1313:             Imc_CA_CAM = 25
1314:             ImcDesc = "Eutrofia (Normal)"
1315:             Exit Function
1317:             ElseIf IMC <= (15.26) Then
1318:             Imc_CA_CAM = 50
1319:             ImcDesc = "Eutrofia (Normal)"
1320:             Exit Function
1322:             ElseIf IMC <= (16.17) Then
1323:             Imc_CA_CAM = 75
1324:             ImcDesc = "Eutrofia (Normal)"
1325:             Exit Function
1327:             ElseIf IMC <= (16.7) Then
1328:             Imc_CA_CAM = 85
1329:             ImcDesc = "Eutrofia (Normal)"
1330:             Exit Function
1332:             End If
1334:             If IMC <= (17.07) Then
1335:             Imc_CA_CAM = 90
1336:             ImcDesc = "Sobrepeso"
1337:             Exit Function
1339:             ElseIf IMC <= (17.66) Then
1340:             Imc_CA_CAM = 95
1341:             ImcDesc = "Sobrepeso"
1342:             Exit Function
1344:             ElseIf IMC < (18.05) Then
1345:             Imc_CA_CAM = 97
1346:             ImcDesc = "Sobrepeso"
1347:             Exit Function
1349:             End If
1351:             If IMC <= (20.36) Then
1352:             Imc_CA_CAM = 99
1353:             ImcDesc = "Obesidade_"
1354:             Exit Function
1355:             End If
1357:             If IMC > (20.36) Then
1358:             Imc_CA_CAM = 99.9
1359:             ImcDesc = "Obesidade_ grave"
1360:             Exit Function
1361:             End If
1364:        End If
1367:        If Idade_ = (6) Then
1368:             If IMC < (12.73) Then
1369:             Imc_CA_CAM = 0.1
1370:             ImcDesc = "Magreza acentuada"
1371:             Exit Function
1372:             End If
1374:             If IMC <= (13.16) Then
1375:             Imc_CA_CAM = 3
1376:             ImcDesc = "Magreza"
1377:             Exit Function
1378:             End If
1380:             If IMC <= (13.39) Then
1381:             Imc_CA_CAM = 5
1382:             ImcDesc = "Eutrofia (Normal)"
1383:             Exit Function
1385:             ElseIf IMC <= (13.77) Then
1386:             Imc_CA_CAM = 10
1387:             ImcDesc = "Eutrofia (Normal)"
1388:             Exit Function
1390:             ElseIf IMC <= (14.04) Then
1391:             Imc_CA_CAM = 15
1392:             ImcDesc = "Eutrofia (Normal)"
1393:             Exit Function
1395:             ElseIf IMC <= (14.46) Then
1396:             Imc_CA_CAM = 25
1397:             ImcDesc = "Eutrofia (Normal)"
1398:             Exit Function
1400:             ElseIf IMC <= (15.31) Then
1401:             Imc_CA_CAM = 50
1402:             ImcDesc = "Eutrofia (Normal)"
1403:             Exit Function
1405:             ElseIf IMC <= (16.26) Then
1406:             Imc_CA_CAM = 75
1407:             ImcDesc = "Eutrofia (Normal)"
1408:             Exit Function
1410:             ElseIf IMC <= (16.82) Then
1411:             Imc_CA_CAM = 85
1412:             ImcDesc = "Eutrofia (Normal)"
1413:             Exit Function
1415:             End If
1417:             If IMC <= (17.22) Then
1418:             Imc_CA_CAM = 90
1419:             ImcDesc = "Sobrepeso"
1420:             Exit Function
1422:             ElseIf IMC <= (17.85) Then
1423:             Imc_CA_CAM = 95
1424:             ImcDesc = "Sobrepeso"
1425:             Exit Function
1427:             ElseIf IMC < (18.29) Then
1428:             Imc_CA_CAM = 97
1429:             ImcDesc = "Sobrepeso"
1430:             Exit Function
1432:             End If
1434:             If IMC <= (20.91) Then
1435:             Imc_CA_CAM = 99
1436:             ImcDesc = "Obesidade_"
1437:             Exit Function
1438:             End If
1440:             If IMC > (20.91) Then
1441:             Imc_CA_CAM = 99.9
1442:             ImcDesc = "Obesidade_ grave"
1443:             Exit Function
1444:             End If
1447:        End If
1449:       If Idade_ = (7) Then
1450:             If IMC < (12.84) Then
1451:             Imc_CA_CAM = 0.1
1452:             ImcDesc = "Magreza acentuada"
1453:             Exit Function
1454:             End If
1456:             If IMC <= (13.27) Then
1457:             Imc_CA_CAM = 3
1458:             ImcDesc = "Magreza"
1459:             Exit Function
1460:             End If
1462:             If IMC <= (13.5) Then
1463:             Imc_CA_CAM = 5
1464:             ImcDesc = "Eutrofia (Normal)"
1465:             Exit Function
1467:             ElseIf IMC <= (13.89) Then
1468:             Imc_CA_CAM = 10
1469:             ImcDesc = "Eutrofia (Normal)"
1470:             Exit Function
1472:             ElseIf IMC <= (14.17) Then
1473:             Imc_CA_CAM = 15
1474:             ImcDesc = "Eutrofia (Normal)"
1475:             Exit Function
1477:             ElseIf IMC <= (14.6) Then
1478:             Imc_CA_CAM = 25
1479:             ImcDesc = "Eutrofia (Normal)"
1480:             Exit Function
1482:             ElseIf IMC <= (15.48) Then
1483:             Imc_CA_CAM = 50
1484:             ImcDesc = "Eutrofia (Normal)"
1485:             Exit Function
1487:             ElseIf IMC <= (16.5) Then
1488:             Imc_CA_CAM = 75
1489:             ImcDesc = "Eutrofia (Normal)"
1490:             Exit Function
1492:             ElseIf IMC <= (17.11) Then
1493:             Imc_CA_CAM = 85
1494:             ImcDesc = "Eutrofia (Normal)"
1495:             Exit Function
1497:             End If
1499:             If IMC <= (17.55) Then
1500:             Imc_CA_CAM = 90
1501:             ImcDesc = "Sobrepeso"
1502:             Exit Function
1504:             ElseIf IMC <= (18.26) Then
1505:             Imc_CA_CAM = 95
1506:             ImcDesc = "Sobrepeso"
1507:             Exit Function
1509:             ElseIf IMC < (18.76) Then
1510:             Imc_CA_CAM = 97
1511:             ImcDesc = "Sobrepeso"
1512:             Exit Function
1514:             End If
1516:             If IMC <= (21.86) Then
1517:             Imc_CA_CAM = 99
1518:             ImcDesc = "Obesidade_"
1519:             Exit Function
1520:             End If
1522:             If IMC > (21.86) Then
1523:             Imc_CA_CAM = 99.9
1524:             ImcDesc = "Obesidade_ grave"
1525:             Exit Function
1526:             End If
1529:        End If
1532:             If Idade_ = (8) Then
1533:             If IMC < (12.99) Then
1534:             Imc_CA_CAM = 0.1
1535:             ImcDesc = "Magreza acentuada"
1536:             Exit Function
1537:             End If
1539:             If IMC <= (13.42) Then
1540:             Imc_CA_CAM = 3
1541:             ImcDesc = "Magreza"
1542:             Exit Function
1543:             End If
1545:             If IMC <= (13.67) Then
1546:             Imc_CA_CAM = 5
1547:             ImcDesc = "Eutrofia (Normal)"
1548:             Exit Function
1550:             ElseIf IMC <= (14.07) Then
1551:             Imc_CA_CAM = 10
1552:             ImcDesc = "Eutrofia (Normal)"
1553:             Exit Function
1555:             ElseIf IMC <= (14.35) Then
1556:             Imc_CA_CAM = 15
1557:             ImcDesc = "Eutrofia (Normal)"
1558:             Exit Function
1560:             ElseIf IMC <= (14.8) Then
1561:             Imc_CA_CAM = 25
1562:             ImcDesc = "Eutrofia (Normal)"
1563:             Exit Function
1565:             ElseIf IMC <= (15.74) Then
1566:             Imc_CA_CAM = 50
1567:             ImcDesc = "Eutrofia (Normal)"
1568:             Exit Function
1570:             ElseIf IMC <= (16.84) Then
1571:             Imc_CA_CAM = 75
1572:             ImcDesc = "Eutrofia (Normal)"
1573:             Exit Function
1575:             ElseIf IMC <= (17.51) Then
1576:             Imc_CA_CAM = 85
1577:             ImcDesc = "Eutrofia (Normal)"
1578:             Exit Function
1580:             End If
1582:             If IMC <= (18) Then
1583:             Imc_CA_CAM = 90
1584:             ImcDesc = "Sobrepeso"
1585:             Exit Function
1587:             ElseIf IMC <= (18.8) Then
1588:             Imc_CA_CAM = 95
1589:             ImcDesc = "Sobrepeso"
1590:             Exit Function
1592:             ElseIf IMC < (19.37) Then
1593:             Imc_CA_CAM = 97
1594:             ImcDesc = "Sobrepeso"
1595:             Exit Function
1597:             End If
1599:             If IMC <= (23.13) Then
1600:             Imc_CA_CAM = 99
1601:             ImcDesc = "Obesidade_"
1602:             Exit Function
1603:             End If
1605:             If IMC > (23.13) Then
1606:             Imc_CA_CAM = 99.9
1607:             ImcDesc = "Obesidade_ grave"
1608:             Exit Function
1609:             End If
1612:        End If
1614:        If Idade_ = (9) Then
1615:             If IMC < (13.17) Then
1616:             Imc_CA_CAM = 0.1
1617:             ImcDesc = "Magreza acentuada"
1618:             Exit Function
1619:             End If
1621:             If IMC <= (13.61) Then
1622:             Imc_CA_CAM = 3
1623:             ImcDesc = "Magreza"
1624:             Exit Function
1625:             End If
1627:             If IMC <= (13.87) Then
1628:             Imc_CA_CAM = 5
1629:             ImcDesc = "Eutrofia (Normal)"
1630:             Exit Function
1632:             ElseIf IMC <= (14.28) Then
1633:             Imc_CA_CAM = 10
1634:             ImcDesc = "Eutrofia (Normal)"
1635:             Exit Function
1637:             ElseIf IMC <= (14.58) Then
1638:             Imc_CA_CAM = 15
1639:             ImcDesc = "Eutrofia (Normal)"
1640:             Exit Function
1642:             ElseIf IMC <= (15.05) Then
1643:             Imc_CA_CAM = 25
1644:             ImcDesc = "Eutrofia (Normal)"
1645:             Exit Function
1647:             ElseIf IMC <= (16.05) Then
1648:             Imc_CA_CAM = 50
1649:             ImcDesc = "Eutrofia (Normal)"
1650:             Exit Function
1652:             ElseIf IMC <= (17.24) Then
1653:             Imc_CA_CAM = 75
1654:             ImcDesc = "Eutrofia (Normal)"
1655:             Exit Function
1657:             ElseIf IMC <= (17.99) Then
1658:             Imc_CA_CAM = 85
1659:             ImcDesc = "Eutrofia (Normal)"
1660:             Exit Function
1662:             End If
1664:             If IMC <= (18.54) Then
1665:             Imc_CA_CAM = 90
1666:             ImcDesc = "Sobrepeso"
1667:             Exit Function
1669:             ElseIf IMC <= (19.45) Then
1670:             Imc_CA_CAM = 95
1671:             ImcDesc = "Sobrepeso"
1672:             Exit Function
1674:             ElseIf IMC < (20.11) Then
1675:             Imc_CA_CAM = 97
1676:             ImcDesc = "Sobrepeso"
1677:             Exit Function
1679:             End If
1681:             If IMC <= (24.73) Then
1682:             Imc_CA_CAM = 99
1683:             ImcDesc = "Obesidade_"
1684:             Exit Function
1685:             End If
1687:             If IMC > (24.73) Then
1688:             Imc_CA_CAM = 99.9
1689:             ImcDesc = "Obesidade_ grave"
1690:             Exit Function
1691:             End If
1694:        End If
1696:              If Idade_ = (10) Then
1697:             If IMC < (13.4) Then
1698:             Imc_CA_CAM = 0.1
1699:             ImcDesc = "Magreza acentuada"
1700:             Exit Function
1701:             End If
1703:             If IMC <= (13.86) Then
1704:             Imc_CA_CAM = 3
1705:             ImcDesc = "Magreza"
1706:             Exit Function
1707:             End If
1709:             If IMC <= (14.13) Then
1710:             Imc_CA_CAM = 5
1711:             ImcDesc = "Eutrofia (Normal)"
1712:             Exit Function
1714:             ElseIf IMC <= (14.56) Then
1715:             Imc_CA_CAM = 10
1716:             ImcDesc = "Eutrofia (Normal)"
1717:             Exit Function
1719:             ElseIf IMC <= (14.88) Then
1720:             Imc_CA_CAM = 15
1721:             ImcDesc = "Eutrofia (Normal)"
1722:             Exit Function
1724:             ElseIf IMC <= (15.38) Then
1725:             Imc_CA_CAM = 25
1726:             ImcDesc = "Eutrofia (Normal)"
1727:             Exit Function
1729:             ElseIf IMC <= (16.44) Then
1730:             Imc_CA_CAM = 50
1731:             ImcDesc = "Eutrofia (Normal)"
1732:             Exit Function
1734:             ElseIf IMC <= (17.74) Then
1735:             Imc_CA_CAM = 75
1736:             ImcDesc = "Eutrofia (Normal)"
1737:             Exit Function
1739:             ElseIf IMC <= (18.57) Then
1740:             Imc_CA_CAM = 85
1741:             ImcDesc = "Eutrofia (Normal)"
1742:             Exit Function
1744:             End If
1746:             If IMC <= (19.19) Then
1747:             Imc_CA_CAM = 90
1748:             ImcDesc = "Sobrepeso"
1749:             Exit Function
1751:             ElseIf IMC <= (20.23) Then
1752:             Imc_CA_CAM = 95
1753:             ImcDesc = "Sobrepeso"
1754:             Exit Function
1756:             ElseIf IMC < (20.99) Then
1757:             Imc_CA_CAM = 97
1758:             ImcDesc = "Sobrepeso"
1759:             Exit Function
1761:             End If
1763:             If IMC <= (26.64) Then
1764:             Imc_CA_CAM = 99
1765:             ImcDesc = "Obesidade_"
1766:             Exit Function
1767:             End If
1769:             If IMC > (26.64) Then
1770:             Imc_CA_CAM = 99.9
1771:             ImcDesc = "Obesidade_ grave"
1772:             Exit Function
1773:             End If
1776:        End If
1778:                     If Idade_ = (11) Then
1779:             If IMC < (13.71) Then
1780:             Imc_CA_CAM = 0.1
1781:             ImcDesc = "Magreza acentuada"
1782:             Exit Function
1783:             End If
1785:             If IMC <= (14.19) Then
1786:             Imc_CA_CAM = 3
1787:             ImcDesc = "Magreza"
1788:             Exit Function
1789:             End If
1791:             If IMC <= (14.47) Then
1792:             Imc_CA_CAM = 5
1793:             ImcDesc = "Eutrofia (Normal)"
1794:             Exit Function
1796:             ElseIf IMC <= (14.93) Then
1797:             Imc_CA_CAM = 10
1798:             ImcDesc = "Eutrofia (Normal)"
1799:             Exit Function
1801:             ElseIf IMC <= (15.26) Then
1802:             Imc_CA_CAM = 15
1803:             ImcDesc = "Eutrofia (Normal)"
1804:             Exit Function
1806:             ElseIf IMC <= (15.79) Then
1807:             Imc_CA_CAM = 25
1808:             ImcDesc = "Eutrofia (Normal)"
1809:             Exit Function
1811:             ElseIf IMC <= (16.94) Then
1812:             Imc_CA_CAM = 50
1813:             ImcDesc = "Eutrofia (Normal)"
1814:             Exit Function
1816:             ElseIf IMC <= (18.35) Then
1817:             Imc_CA_CAM = 75
1818:             ImcDesc = "Eutrofia (Normal)"
1819:             Exit Function
1821:             ElseIf IMC <= (19.26) Then
1822:             Imc_CA_CAM = 85
1823:             ImcDesc = "Eutrofia (Normal)"
1824:             Exit Function
1826:             End If
1828:             If IMC <= (19.95) Then
1829:             Imc_CA_CAM = 90
1830:             ImcDesc = "Sobrepeso"
1831:             Exit Function
1833:             ElseIf IMC <= (21.11) Then
1834:             Imc_CA_CAM = 95
1835:             ImcDesc = "Sobrepeso"
1836:             Exit Function
1838:             ElseIf IMC < (21.98) Then
1839:             Imc_CA_CAM = 97
1840:             ImcDesc = "Sobrepeso"
1841:             Exit Function
1843:             End If
1845:             If IMC <= (28.74) Then
1846:             Imc_CA_CAM = 99
1847:             ImcDesc = "Obesidade_"
1848:             Exit Function
1849:             End If
1851:             If IMC > (28.74) Then
1852:             Imc_CA_CAM = 99.9
1853:             ImcDesc = "Obesidade_ grave"
1854:             Exit Function
1855:             End If
1858:        End If
1860:                     If Idade_ = (12) Then
1861:             If IMC < (14.08) Then
1862:             Imc_CA_CAM = 0.1
1863:             ImcDesc = "Magreza acentuada"
1864:             Exit Function
1865:             End If
1867:             If IMC <= (14.6) Then
1868:             Imc_CA_CAM = 3
1869:             ImcDesc = "Magreza"
1870:             Exit Function
1871:             End If
1873:             If IMC <= (14.89) Then
1874:             Imc_CA_CAM = 5
1875:             ImcDesc = "Eutrofia (Normal)"
1876:             Exit Function
1878:             ElseIf IMC <= (15.38) Then
1879:             Imc_CA_CAM = 10
1880:             ImcDesc = "Eutrofia (Normal)"
1881:             Exit Function
1883:             ElseIf IMC <= (15.73) Then
1884:             Imc_CA_CAM = 15
1885:             ImcDesc = "Eutrofia (Normal)"
1886:             Exit Function
1888:             ElseIf IMC <= (16.3) Then
1889:             Imc_CA_CAM = 25
1890:             ImcDesc = "Eutrofia (Normal)"
1891:             Exit Function
1893:             ElseIf IMC <= (17.53) Then
1894:             Imc_CA_CAM = 50
1895:             ImcDesc = "Eutrofia (Normal)"
1896:             Exit Function
1898:             ElseIf IMC <= (19.06) Then
1899:             Imc_CA_CAM = 75
1900:             ImcDesc = "Eutrofia (Normal)"
1901:             Exit Function
1903:             ElseIf IMC <= (20.05) Then
1904:             Imc_CA_CAM = 85
1905:             ImcDesc = "Eutrofia (Normal)"
1906:             Exit Function
1908:             End If
1910:             If IMC <= (20.81) Then
1911:             Imc_CA_CAM = 90
1912:             ImcDesc = "Sobrepeso"
1913:             Exit Function
1915:             ElseIf IMC <= (22.09) Then
1916:             Imc_CA_CAM = 95
1917:             ImcDesc = "Sobrepeso"
1918:             Exit Function
1920:             ElseIf IMC < (23.05) Then
1921:             Imc_CA_CAM = 97
1922:             ImcDesc = "Sobrepeso"
1923:             Exit Function
1925:             End If
1927:             If IMC <= (30.79) Then
1928:             Imc_CA_CAM = 99
1929:             ImcDesc = "Obesidade_"
1930:             Exit Function
1931:             End If
1933:             If IMC > (30.79) Then
1934:             Imc_CA_CAM = 99.9
1935:             ImcDesc = "Obesidade_ grave"
1936:             Exit Function
1937:             End If
1940:        End If
1942:                     If Idade_ = (13) Then
1943:             If IMC < (14.54) Then
1944:             Imc_CA_CAM = 0.1
1945:             ImcDesc = "Magreza acentuada"
1946:             Exit Function
1947:             End If
1949:             If IMC <= (15.09) Then
1950:             Imc_CA_CAM = 3
1951:             ImcDesc = "Magreza"
1952:             Exit Function
1953:             End If
1955:             If IMC <= (15.4) Then
1956:             Imc_CA_CAM = 5
1957:             ImcDesc = "Eutrofia (Normal)"
1958:             Exit Function
1960:             ElseIf IMC <= (15.92) Then
1961:             Imc_CA_CAM = 10
1962:             ImcDesc = "Eutrofia (Normal)"
1963:             Exit Function
1965:             ElseIf IMC <= (16.3) Then
1966:             Imc_CA_CAM = 15
1967:             ImcDesc = "Eutrofia (Normal)"
1968:             Exit Function
1970:             ElseIf IMC <= (16.91) Then
1971:             Imc_CA_CAM = 25
1972:             ImcDesc = "Eutrofia (Normal)"
1973:             Exit Function
1975:             ElseIf IMC <= (18.23) Then
1976:             Imc_CA_CAM = 50
1977:             ImcDesc = "Eutrofia (Normal)"
1978:             Exit Function
1980:             ElseIf IMC <= (19.88) Then
1981:             Imc_CA_CAM = 75
1982:             ImcDesc = "Eutrofia (Normal)"
1983:             Exit Function
1985:             ElseIf IMC <= (20.94) Then
1986:             Imc_CA_CAM = 85
1987:             ImcDesc = "Eutrofia (Normal)"
1988:             Exit Function
1990:             End If
1992:             If IMC <= (21.76) Then
1993:             Imc_CA_CAM = 90
1994:             ImcDesc = "Sobrepeso"
1995:             Exit Function
1997:             ElseIf IMC <= (23.14) Then
1998:             Imc_CA_CAM = 95
1999:             ImcDesc = "Sobrepeso"
2000:             Exit Function
2002:             ElseIf IMC < (24.18) Then
2003:             Imc_CA_CAM = 97
2004:             ImcDesc = "Sobrepeso"
2005:             Exit Function
2007:             End If
2009:             If IMC <= (32.6) Then
2010:             Imc_CA_CAM = 99
2011:             ImcDesc = "Obesidade_"
2012:             Exit Function
2013:             End If
2015:             If IMC > (32.6) Then
2016:             Imc_CA_CAM = 99.9
2017:             ImcDesc = "Obesidade_ grave"
2018:             Exit Function
2019:             End If
2022:        End If
2024:                     If Idade_ = (14) Then
2025:             If IMC < (15.05) Then
2026:             Imc_CA_CAM = 0.1
2027:             ImcDesc = "Magreza acentuada"
2028:             Exit Function
2029:             End If
2031:             If IMC <= (15.64) Then
2032:             Imc_CA_CAM = 3
2033:             ImcDesc = "Magreza"
2034:             Exit Function
2035:             End If
2037:             If IMC <= (15.98) Then
2038:             Imc_CA_CAM = 5
2039:             ImcDesc = "Eutrofia (Normal)"
2040:             Exit Function
2042:             ElseIf IMC <= (16.53) Then
2043:             Imc_CA_CAM = 10
2044:             ImcDesc = "Eutrofia (Normal)"
2045:             Exit Function
2047:             ElseIf IMC <= (16.94) Then
2048:             Imc_CA_CAM = 15
2049:             ImcDesc = "Eutrofia (Normal)"
2050:             Exit Function
2052:             ElseIf IMC <= (17.59) Then
2053:             Imc_CA_CAM = 25
2054:             ImcDesc = "Eutrofia (Normal)"
2055:             Exit Function
2057:             ElseIf IMC <= (19.01) Then
2058:             Imc_CA_CAM = 50
2059:             ImcDesc = "Eutrofia (Normal)"
2060:             Exit Function
2062:             ElseIf IMC <= (20.76) Then
2063:             Imc_CA_CAM = 75
2064:             ImcDesc = "Eutrofia (Normal)"
2065:             Exit Function
2067:             ElseIf IMC <= (21.89) Then
2068:             Imc_CA_CAM = 85
2069:             ImcDesc = "Eutrofia (Normal)"
2070:             Exit Function
2072:             End If
2074:             If IMC <= (22.76) Then
2075:             Imc_CA_CAM = 90
2076:             ImcDesc = "Sobrepeso"
2077:             Exit Function
2079:             ElseIf IMC <= (24.22) Then
2080:             Imc_CA_CAM = 95
2081:             ImcDesc = "Sobrepeso"
2082:             Exit Function
2084:             ElseIf IMC < (25.32) Then
2085:             Imc_CA_CAM = 97
2086:             ImcDesc = "Sobrepeso"
2087:             Exit Function
2089:             End If
2091:             If IMC <= (34.01) Then
2092:             Imc_CA_CAM = 99
2093:             ImcDesc = "Obesidade_"
2094:             Exit Function
2095:             End If
2097:             If IMC > (34.01) Then
2098:             Imc_CA_CAM = 99.9
2099:             ImcDesc = "Obesidade_ grave"
2100:             Exit Function
2101:             End If
2104:        End If
2106:                     If Idade_ = (15) Then
2107:             If IMC < (15.56) Then
2108:             Imc_CA_CAM = 0.1
2109:             ImcDesc = "Magreza acentuada"
2110:             Exit Function
2111:             End If
2113:             If IMC <= (16.19) Then
2114:             Imc_CA_CAM = 3
2115:             ImcDesc = "Magreza"
2116:             Exit Function
2117:             End If
2119:             If IMC <= (16.55) Then
2120:             Imc_CA_CAM = 5
2121:             ImcDesc = "Eutrofia (Normal)"
2122:             Exit Function
2124:             ElseIf IMC <= (17.15) Then
2125:             Imc_CA_CAM = 10
2126:             ImcDesc = "Eutrofia (Normal)"
2127:             Exit Function
2129:             ElseIf IMC <= (17.58) Then
2130:             Imc_CA_CAM = 15
2131:             ImcDesc = "Eutrofia (Normal)"
2132:             Exit Function
2134:             ElseIf IMC <= (18.28) Then
2135:             Imc_CA_CAM = 25
2136:             ImcDesc = "Eutrofia (Normal)"
2137:             Exit Function
2139:             ElseIf IMC <= (19.77) Then
2140:             Imc_CA_CAM = 50
2141:             ImcDesc = "Eutrofia (Normal)"
2142:             Exit Function
2144:             ElseIf IMC <= (21.63) Then
2145:             Imc_CA_CAM = 75
2146:             ImcDesc = "Eutrofia (Normal)"
2147:             Exit Function
2149:             ElseIf IMC <= (22.81) Then
2150:             Imc_CA_CAM = 85
2151:             ImcDesc = "Eutrofia (Normal)"
2152:             Exit Function
2154:             End If
2156:             If IMC <= (23.71) Then
2157:             Imc_CA_CAM = 90
2158:             ImcDesc = "Sobrepeso"
2159:             Exit Function
2161:             ElseIf IMC <= (25.23) Then
2162:             Imc_CA_CAM = 95
2163:             ImcDesc = "Sobrepeso"
2164:             Exit Function
2166:             ElseIf IMC < (26.35) Then
2167:             Imc_CA_CAM = 97
2168:             ImcDesc = "Sobrepeso"
2169:             Exit Function
2171:             End If
2173:             If IMC <= (34.97) Then
2174:             Imc_CA_CAM = 99
2175:             ImcDesc = "Obesidade_"
2176:             Exit Function
2177:             End If
2179:             If IMC > (34.97) Then
2180:             Imc_CA_CAM = 99.9
2181:             ImcDesc = "Obesidade_ grave"
2182:             Exit Function
2183:             End If
2186:        End If
2188:                     If Idade_ = (16) Then
2189:             If IMC < (16.02) Then
2190:             Imc_CA_CAM = 0.1
2191:             ImcDesc = "Magreza acentuada"
2192:             Exit Function
2193:             End If
2195:             If IMC <= (16.69) Then
2196:             Imc_CA_CAM = 3
2197:             ImcDesc = "Magreza"
2198:             Exit Function
2199:             End If
2201:             If IMC <= (17.08) Then
2202:             Imc_CA_CAM = 5
2203:             ImcDesc = "Eutrofia (Normal)"
2204:             Exit Function
2206:             ElseIf IMC <= (17.71) Then
2207:             Imc_CA_CAM = 10
2208:             ImcDesc = "Eutrofia (Normal)"
2209:             Exit Function
2211:             ElseIf IMC <= (18.18) Then
2212:             Imc_CA_CAM = 15
2213:             ImcDesc = "Eutrofia (Normal)"
2214:             Exit Function
2216:             ElseIf IMC <= (18.91) Then
2217:             Imc_CA_CAM = 25
2218:             ImcDesc = "Eutrofia (Normal)"
2219:             Exit Function
2221:             ElseIf IMC <= (20.5) Then
2222:             Imc_CA_CAM = 50
2223:             ImcDesc = "Eutrofia (Normal)"
2224:             Exit Function
2226:             ElseIf IMC <= (22.43) Then
2227:             Imc_CA_CAM = 75
2228:             ImcDesc = "Eutrofia (Normal)"
2229:             Exit Function
2231:             ElseIf IMC <= (23.66) Then
2232:             Imc_CA_CAM = 85
2233:             ImcDesc = "Eutrofia (Normal)"
2234:             Exit Function
2236:             End If
2238:             If IMC <= (24.58) Then
2239:             Imc_CA_CAM = 90
2240:             ImcDesc = "Sobrepeso"
2241:             Exit Function
2243:             ElseIf IMC <= (26.13) Then
2244:             Imc_CA_CAM = 95
2245:             ImcDesc = "Sobrepeso"
2246:             Exit Function
2248:             ElseIf IMC < (27.26) Then
2249:             Imc_CA_CAM = 97
2250:             ImcDesc = "Sobrepeso"
2251:             Exit Function
2252:             End If
2254:             If IMC <= (35.58) Then
2255:             Imc_CA_CAM = 99
2256:             ImcDesc = "Obesidade_"
2257:             Exit Function
2258:             End If
2260:             If IMC > (35.58) Then
2261:             Imc_CA_CAM = 99.9
2262:             ImcDesc = "Obesidade_ grave"
2263:             Exit Function
2264:             End If
2267:        End If
2269:                     If Idade_ = (17) Then
2270:             If IMC < (16.41) Then
2271:             Imc_CA_CAM = 0.1
2272:             ImcDesc = "Magreza acentuada"
2273:             Exit Function
2274:             End If
2276:             If IMC <= (17.13) Then
2277:             Imc_CA_CAM = 3
2278:             ImcDesc = "Magreza"
2279:             Exit Function
2280:             End If
2282:             If IMC <= (17.54) Then
2283:             Imc_CA_CAM = 5
2284:             ImcDesc = "Eutrofia (Normal)"
2285:             Exit Function
2287:             ElseIf IMC <= (18.22) Then
2288:             Imc_CA_CAM = 10
2289:             ImcDesc = "Eutrofia (Normal)"
2290:             Exit Function
2292:             ElseIf IMC <= (18.71) Then
2293:             Imc_CA_CAM = 15
2294:             ImcDesc = "Eutrofia (Normal)"
2295:             Exit Function
2297:             ElseIf IMC <= (19.49) Then
2298:             Imc_CA_CAM = 25
2299:             ImcDesc = "Eutrofia (Normal)"
2300:             Exit Function
2302:             ElseIf IMC <= (21.14) Then
2303:             Imc_CA_CAM = 50
2304:             ImcDesc = "Eutrofia (Normal)"
2305:             Exit Function
2307:             ElseIf IMC <= (23.15) Then
2308:             Imc_CA_CAM = 75
2309:             ImcDesc = "Eutrofia (Normal)"
2310:             Exit Function
2312:             ElseIf IMC <= (24.4) Then
2313:             Imc_CA_CAM = 85
2314:             ImcDesc = "Eutrofia (Normal)"
2315:             Exit Function
2317:             End If
2319:             If IMC <= (25.34) Then
2320:             Imc_CA_CAM = 90
2321:             ImcDesc = "Sobrepeso"
2322:             Exit Function
2324:             ElseIf IMC <= (26.9) Then
2325:             Imc_CA_CAM = 95
2326:             ImcDesc = "Sobrepeso"
2327:             Exit Function
2329:             ElseIf IMC < (28.02) Then
2330:             Imc_CA_CAM = 97
2331:             ImcDesc = "Sobrepeso"
2332:             Exit Function
2334:             End If
2336:             If IMC <= (35.95) Then
2337:             Imc_CA_CAM = 99
2338:             ImcDesc = "Obesidade_"
2339:             Exit Function
2340:             End If
2342:             If IMC > (35.95) Then
2343:             Imc_CA_CAM = 99.9
2344:             ImcDesc = "Obesidade_ grave"
2345:             Exit Function
2346:             End If
2349:        End If
2351:                     If Idade_ = (18) Then
2352:             If IMC < (16.73) Then
2353:             Imc_CA_CAM = 0.1
2354:             ImcDesc = "Magreza acentuada"
2355:             Exit Function
2356:             End If
2358:             If IMC <= (17.5) Then
2359:             Imc_CA_CAM = 3
2360:             ImcDesc = "Magreza"
2361:             Exit Function
2362:             End If
2364:             If IMC <= (17.93) Then
2365:             Imc_CA_CAM = 5
2366:             ImcDesc = "Eutrofia (Normal)"
2367:             Exit Function
2369:             ElseIf IMC <= (18.65) Then
2370:             Imc_CA_CAM = 10
2371:             ImcDesc = "Eutrofia (Normal)"
2372:             Exit Function
2374:             ElseIf IMC <= (19.16) Then
2375:             Imc_CA_CAM = 15
2376:             ImcDesc = "Eutrofia (Normal)"
2377:             Exit Function
2379:             ElseIf IMC <= (19.98) Then
2380:             Imc_CA_CAM = 25
2381:             ImcDesc = "Eutrofia (Normal)"
2382:             Exit Function
2384:             ElseIf IMC <= (21.71) Then
2385:             Imc_CA_CAM = 50
2386:             ImcDesc = "Eutrofia (Normal)"
2387:             Exit Function
2389:             ElseIf IMC <= (23.77) Then
2390:             Imc_CA_CAM = 75
2391:             ImcDesc = "Eutrofia (Normal)"
2392:             Exit Function
2394:             ElseIf IMC <= (25.05) Then
2395:             Imc_CA_CAM = 85
2396:             ImcDesc = "Eutrofia (Normal)"
2397:             Exit Function
2399:             End If
2401:             If IMC <= (25.99) Then
2402:             Imc_CA_CAM = 90
2403:             ImcDesc = "Sobrepeso"
2404:             Exit Function
2406:             ElseIf IMC <= (27.54) Then
2407:             Imc_CA_CAM = 95
2408:             ImcDesc = "Sobrepeso"
2409:             Exit Function
2411:             ElseIf IMC < (28.65) Then
2412:             Imc_CA_CAM = 97
2413:             ImcDesc = "Sobrepeso"
2414:             Exit Function
2416:             End If
2418:             If IMC <= (36.12) Then
2419:             Imc_CA_CAM = 99
2420:             ImcDesc = "Obesidade_"
2421:             Exit Function
2422:             End If
2424:             If IMC > (36.12) Then
2425:             Imc_CA_CAM = 99.9
2426:             ImcDesc = "Obesidade_ grave"
2427:             Exit Function
2428:             End If
2431:        End If
2433:                     If Idade_ = (19) Then
2434:             If IMC < (16.96) Then
2435:             Imc_CA_CAM = 0.1
2436:             ImcDesc = "Magreza acentuada"
2437:             Exit Function
2438:             End If
2440:             If IMC <= (17.78) Then
2441:             Imc_CA_CAM = 3
2442:             ImcDesc = "Magreza"
2443:             Exit Function
2444:             End If
2446:             If IMC <= (18.24) Then
2447:             Imc_CA_CAM = 5
2448:             ImcDesc = "Eutrofia (Normal)"
2449:             Exit Function
2451:             ElseIf IMC <= (19) Then
2452:             Imc_CA_CAM = 10
2453:             ImcDesc = "Eutrofia (Normal)"
2454:             Exit Function
2456:             ElseIf IMC <= (19.54) Then
2457:             Imc_CA_CAM = 15
2458:             ImcDesc = "Eutrofia (Normal)"
2459:             Exit Function
2461:             ElseIf IMC <= (20.4) Then
2462:             Imc_CA_CAM = 25
2463:             ImcDesc = "Eutrofia (Normal)"
2464:             Exit Function
2466:             ElseIf IMC <= (22.19) Then
2467:             Imc_CA_CAM = 50
2468:             ImcDesc = "Eutrofia (Normal)"
2469:             Exit Function
2471:             ElseIf IMC <= (24.3) Then
2472:             Imc_CA_CAM = 75
2473:             ImcDesc = "Eutrofia (Normal)"
2474:             Exit Function
2476:             ElseIf IMC <= (25.58) Then
2477:             Imc_CA_CAM = 85
2478:             ImcDesc = "Eutrofia (Normal)"
2479:             Exit Function
2481:             End If
2483:             If IMC <= (26.53) Then
2484:             Imc_CA_CAM = 90
2485:             ImcDesc = "Sobrepeso"
2486:             Exit Function
2488:             ElseIf IMC <= (28.06) Then
2489:             Imc_CA_CAM = 95
2490:             ImcDesc = "Sobrepeso"
2491:             Exit Function
2493:             ElseIf IMC < (29.14) Then
2494:             Imc_CA_CAM = 97
2495:             ImcDesc = "Sobrepeso"
2496:             Exit Function
2498:             End If
2500:             If IMC <= (36.14) Then
2501:             Imc_CA_CAM = 99
2502:             ImcDesc = "Obesidade_"
2503:             Exit Function
2504:             End If
2506:             If IMC > (36.14) Then
2507:             Imc_CA_CAM = 99.9
2508:             ImcDesc = "Obesidade_ grave"
2509:             Exit Function
2510:             End If
2513:        End If
2515:     End If
2518: End Function
```

## GORDURA.bas — TabelaGordura

Origem: `GORDURA.bas:3`.

```text
3: Public Function TabelaGordura(Pgordura As Double, sexo_ As String, Idade_ As Double) As String
6: If sexo_ = "Masculino" Then
18:     If Idade_ < (18) Then
21:          If Pgordura < (5) Then
22:             TabelaGordura = "Muito Baixo"
23:             Exit Function
25:             ElseIf Pgordura <= (10) Then
26:             TabelaGordura = "Baixo"
27:             Exit Function
29:             ElseIf Pgordura <= (20) Then
30:             TabelaGordura = "Ótimo"
31:             Exit Function
33:             ElseIf Pgordura <= (25) Then
34:             TabelaGordura = "Moderadamente Alto"
35:             Exit Function
37:             ElseIf Pgordura <= (31) Then
38:             TabelaGordura = "Alto"
39:             Exit Function
41:             ElseIf Pgordura > (31) Then
42:             TabelaGordura = "Muito Alto"
43:             Exit Function
45:           End If
46:     End If
50:     If Idade_ <= (25) Then
52:              If Pgordura < (7) Then
53:             TabelaGordura = "Excelente"
54:             Exit Function
56:             ElseIf Pgordura < (11) Then
57:             TabelaGordura = "Bom"
58:             Exit Function
60:             ElseIf Pgordura < (13.5) Then
61:             TabelaGordura = "Acima da Média"
62:             Exit Function
64:             ElseIf Pgordura < (16.5) Then
65:             TabelaGordura = "Média"
66:             Exit Function
68:             ElseIf Pgordura < (20) Then
69:             TabelaGordura = "ABaixo da Média"
70:             Exit Function
72:             ElseIf Pgordura < (25) Then
73:             TabelaGordura = "Ruim"
74:             Exit Function
76:             ElseIf Pgordura >= (25) Then
77:             TabelaGordura = "Muito Ruim"
78:             Exit Function
80:           End If
81:     End If
85:     If Idade_ <= (35) Then
86:          If Pgordura < (11.5) Then
87:             TabelaGordura = "Excelente"
88:             Exit Function
90:             ElseIf Pgordura < (15.5) Then
91:             TabelaGordura = "Bom"
92:             Exit Function
94:             ElseIf Pgordura < (18) Then
95:             TabelaGordura = "Acima da Média"
96:             Exit Function
98:             ElseIf Pgordura < (21) Then
99:             TabelaGordura = "Média"
100:             Exit Function
102:             ElseIf Pgordura < (24.5) Then
103:             TabelaGordura = "ABaixo da Média"
104:             Exit Function
106:             ElseIf Pgordura < (28) Then
107:             TabelaGordura = "Ruim"
108:             Exit Function
110:             ElseIf Pgordura >= (28) Then
111:             TabelaGordura = "Muito Ruim"
112:             Exit Function
114:           End If
115:     End If
118:     If Idade_ <= (45) Then
119:          If Pgordura < (15) Then
120:             TabelaGordura = "Excelente"
121:             Exit Function
123:             ElseIf Pgordura < (18.5) Then
124:             TabelaGordura = "Bom"
125:             Exit Function
127:             ElseIf Pgordura < (21) Then
128:             TabelaGordura = "Acima da Média"
129:             Exit Function
131:             ElseIf Pgordura < (23.5) Then
132:             TabelaGordura = "Média"
133:             Exit Function
135:             ElseIf Pgordura < (26) Then
136:             TabelaGordura = "ABaixo da Média"
137:             Exit Function
139:             ElseIf Pgordura < (29.5) Then
140:             TabelaGordura = "Ruim"
141:             Exit Function
143:             ElseIf Pgordura >= (29.5) Then
144:             TabelaGordura = "Muito Ruim"
145:             Exit Function
147:           End If
148:     End If
150:     If Idade_ <= (55) Then
151:          If Pgordura < (17) Then
152:             TabelaGordura = "Excelente"
153:             Exit Function
155:             ElseIf Pgordura < (20.5) Then
156:             TabelaGordura = "Bom"
157:             Exit Function
159:             ElseIf Pgordura < (23.5) Then
160:             TabelaGordura = "Acima da Média"
161:             Exit Function
163:             ElseIf Pgordura < (25.5) Then
164:             TabelaGordura = "Média"
165:             Exit Function
167:             ElseIf Pgordura < (27.5) Then
168:             TabelaGordura = "ABaixo da Média"
169:             Exit Function
171:             ElseIf Pgordura < (31) Then
172:             TabelaGordura = "Ruim"
173:             Exit Function
175:             ElseIf Pgordura >= (31) Then
176:             TabelaGordura = "Muito Ruim"
177:             Exit Function
179:           End If
180:     End If
182:     If Idade_ <= (65) Then
183:          If Pgordura < (19) Then
184:             TabelaGordura = "Excelente"
185:             Exit Function
187:             ElseIf Pgordura < (21.5) Then
188:             TabelaGordura = "Bom"
189:             Exit Function
191:             ElseIf Pgordura < (23.5) Then
192:             TabelaGordura = "Acima da Média"
193:             Exit Function
195:             ElseIf Pgordura < (25.5) Then
196:             TabelaGordura = "Média"
197:             Exit Function
199:             ElseIf Pgordura < (27.5) Then
200:             TabelaGordura = "ABaixo da Média"
201:             Exit Function
203:             ElseIf Pgordura < (31) Then
204:             TabelaGordura = "Ruim"
205:             Exit Function
207:             ElseIf Pgordura >= (31) Then
208:             TabelaGordura = "Muito Ruim"
209:             Exit Function
211:           End If
212:     End If
214:           If Idade_ > (65) Then
215:           MsgBox "Não foi encontrada CLASSIFICAÇÃO para esse avaliado. Insira manualmente a CLASSIFICAÇÃO para prosseguir!"
216:            TabelaGordura = "N/A"
217:           End If
221: ElseIf sexo_ = "Feminino" Then
232:     If Idade_ < (18) Then
235:          If Pgordura < (12) Then
236:             TabelaGordura = "Muito Baixo"
237:             Exit Function
239:             ElseIf Pgordura <= (15) Then
240:             TabelaGordura = "Baixo"
241:             Exit Function
243:             ElseIf Pgordura <= (25) Then
244:             TabelaGordura = "Ótimo"
245:             Exit Function
247:             ElseIf Pgordura <= (30) Then
248:             TabelaGordura = "Moderadamente Alto"
249:             Exit Function
251:             ElseIf Pgordura <= (36) Then
252:             TabelaGordura = "Alto"
253:             Exit Function
255:             ElseIf Pgordura > (36) Then
256:             TabelaGordura = "Muito Alto"
257:             Exit Function
259:           End If
260:     End If
264:     If Idade_ <= (25) Then
265:          If Pgordura < (16.5) Then
266:             TabelaGordura = "Excelente"
267:             Exit Function
269:             ElseIf Pgordura < (19.5) Then
270:             TabelaGordura = "Bom"
271:             Exit Function
273:             ElseIf Pgordura < (22.5) Then
274:             TabelaGordura = "Acima da Média"
275:             Exit Function
277:             ElseIf Pgordura < (25.5) Then
278:             TabelaGordura = "Média"
279:             Exit Function
281:             ElseIf Pgordura < (28.5) Then
282:             TabelaGordura = "ABaixo da Média"
283:             Exit Function
285:             ElseIf Pgordura < (32) Then
286:             TabelaGordura = "Ruim"
287:             Exit Function
289:             ElseIf Pgordura >= (32) Then
290:             TabelaGordura = "Muito Ruim"
291:             Exit Function
293:           End If
294:     End If
297:     If Idade_ <= (35) Then
298:          If Pgordura < (17) Then
299:             TabelaGordura = "Excelente"
300:             Exit Function
302:             ElseIf Pgordura < (20.5) Then
303:             TabelaGordura = "Bom"
304:             Exit Function
306:             ElseIf Pgordura < (23.5) Then
307:             TabelaGordura = "Acima da Média"
308:             Exit Function
310:             ElseIf Pgordura < (26) Then
311:             TabelaGordura = "Média"
312:             Exit Function
314:             ElseIf Pgordura < (30) Then
315:             TabelaGordura = "ABaixo da Média"
316:             Exit Function
318:             ElseIf Pgordura < (34.5) Then
319:             TabelaGordura = "Ruim"
320:             Exit Function
322:             ElseIf Pgordura >= (34.5) Then
323:             TabelaGordura = "Muito Ruim"
324:             Exit Function
326:           End If
327:     End If
329:     If Idade_ <= (45) Then
330:          If Pgordura < (19.5) Then
331:             TabelaGordura = "Excelente"
332:             Exit Function
334:             ElseIf Pgordura < (23.5) Then
335:             TabelaGordura = "Bom"
336:             Exit Function
338:             ElseIf Pgordura < (26.5) Then
339:             TabelaGordura = "Acima da Média"
340:             Exit Function
342:             ElseIf Pgordura < (29.5) Then
343:             TabelaGordura = "Média"
344:             Exit Function
346:             ElseIf Pgordura < (32.5) Then
347:             TabelaGordura = "ABaixo da Média"
348:             Exit Function
350:             ElseIf Pgordura < (37) Then
351:             TabelaGordura = "Ruim"
352:             Exit Function
354:             ElseIf Pgordura >= (37) Then
355:             TabelaGordura = "Muito Ruim"
356:             Exit Function
358:           End If
359:     End If
362:     If Idade_ <= (55) Then
363:          If Pgordura < (22) Then
364:             TabelaGordura = "Excelente"
365:             Exit Function
367:             ElseIf Pgordura < (25.5) Then
368:             TabelaGordura = "Bom"
369:             Exit Function
371:             ElseIf Pgordura < (28.5) Then
372:             TabelaGordura = "Acima da Média"
373:             Exit Function
375:             ElseIf Pgordura < (31.5) Then
376:             TabelaGordura = "Média"
377:             Exit Function
379:             ElseIf Pgordura < (34.5) Then
380:             TabelaGordura = "ABaixo da Média"
381:             Exit Function
383:             ElseIf Pgordura < (38.5) Then
384:             TabelaGordura = "Ruim"
385:             Exit Function
387:             ElseIf Pgordura >= (38.5) Then
388:             TabelaGordura = "Muito Ruim"
389:             Exit Function
391:           End If
392:     End If
394:     If Idade_ <= (65) Then
395:          If Pgordura < (23) Then
396:             TabelaGordura = "Excelente"
397:             Exit Function
399:             ElseIf Pgordura < (26.5) Then
400:             TabelaGordura = "Bom"
401:             Exit Function
403:             ElseIf Pgordura < (29.5) Then
404:             TabelaGordura = "Acima da Média"
405:             Exit Function
407:             ElseIf Pgordura < (32.5) Then
408:             TabelaGordura = "Média"
409:             Exit Function
411:             ElseIf Pgordura < (35.5) Then
412:             TabelaGordura = "ABaixo da Média"
413:             Exit Function
415:             ElseIf Pgordura < (38.5) Then
416:             TabelaGordura = "Ruim"
417:             Exit Function
419:             ElseIf Pgordura >= (38.5) Then
420:             TabelaGordura = "Muito Ruim"
421:             Exit Function
422:           End If
423:     End If
425:           If Idade_ > (65) Then
426:            MsgBox "Não foi encontrada CLASSIFICAÇÃO para esse avaliado. Insira manualmente a CLASSIFICAÇÃO para prosseguir!"
427:            TabelaGordura = "N/A"
428:            Exit Function
429:           End If
430: End If
432: End Function
```

## GORDURA.bas — TabelaGorduraAlvo

Origem: `GORDURA.bas:433`.

```text
433: Public Function TabelaGorduraAlvo(sexo_ As String, Idade_ As Double, Pgordura As Double) As Double
435: If sexo_ = "Masculino" Then
437:     If Idade_ <= (25) Then
438:          If Pgordura < (7) Then
439:             TabelaGorduraAlvo = 5
440:             Exit Function
442:             ElseIf Pgordura < (11) Then
443:             TabelaGorduraAlvo = 9
444:             Exit Function
446:             ElseIf Pgordura < (14) Then
447:             TabelaGorduraAlvo = 12.5
448:             Exit Function
450:             ElseIf Pgordura <= (17) Then
451:             TabelaGorduraAlvo = 15
452:             Exit Function
454:             ElseIf Pgordura < (20) Then
455:             TabelaGorduraAlvo = 15
456:             Exit Function
458:             ElseIf Pgordura < (25) Then
459:             TabelaGorduraAlvo = 15
460:             Exit Function
462:             ElseIf Pgordura > (25) Then
463:             TabelaGorduraAlvo = 15
464:             Exit Function
466:           End If
467:     End If
470:     If Idade_ <= (35) Then
471:          If Pgordura < (11.5) Then
472:             TabelaGorduraAlvo = 9.5
473:             Exit Function
475:             ElseIf Pgordura < (15.5) Then
476:             TabelaGorduraAlvo = 13.5
477:             Exit Function
479:             ElseIf Pgordura < (18) Then
480:             TabelaGorduraAlvo = 17
481:             Exit Function
483:             ElseIf Pgordura < (21) Then
484:             TabelaGorduraAlvo = 19
485:             Exit Function
487:             ElseIf Pgordura < (24) Then
488:             TabelaGorduraAlvo = 19
489:             Exit Function
491:             ElseIf Pgordura < (27.5) Then
492:             TabelaGorduraAlvo = 19
493:             Exit Function
495:             ElseIf Pgordura > (27.5) Then
496:             TabelaGorduraAlvo = 19
497:             Exit Function
499:           End If
500:     End If
502:     If Idade_ <= (45) Then
503:          If Pgordura < (15) Then
504:             TabelaGorduraAlvo = 12
505:             Exit Function
507:             ElseIf Pgordura < (18.5) Then
508:             TabelaGorduraAlvo = 17
509:             Exit Function
511:             ElseIf Pgordura < (21) Then
512:             TabelaGorduraAlvo = 20
513:             Exit Function
515:             ElseIf Pgordura < (23.5) Then
516:             TabelaGorduraAlvo = 22
517:             Exit Function
519:             ElseIf Pgordura < (26) Then
520:             TabelaGorduraAlvo = 22
521:             Exit Function
523:             ElseIf Pgordura < (29.5) Then
524:             TabelaGorduraAlvo = 22
525:             Exit Function
527:             ElseIf Pgordura > (29.5) Then
528:             TabelaGorduraAlvo = 22
529:             Exit Function
531:           End If
532:     End If
534:     If Idade_ <= (55) Then
535:          If Pgordura < (17) Then
536:             TabelaGorduraAlvo = 14
537:             Exit Function
539:             ElseIf Pgordura < (20.5) Then
540:             TabelaGorduraAlvo = 19
541:             Exit Function
543:             ElseIf Pgordura < (23.5) Then
544:             TabelaGorduraAlvo = 22
545:             Exit Function
547:             ElseIf Pgordura < (23.5) Then
548:             TabelaGorduraAlvo = 25
549:             Exit Function
551:             ElseIf Pgordura < (25.5) Then
552:             TabelaGorduraAlvo = 25
553:             Exit Function
555:             ElseIf Pgordura < (27.5) Then
556:             TabelaGorduraAlvo = 25
557:             Exit Function
559:             ElseIf Pgordura > (31) Then
560:             TabelaGorduraAlvo = 25
561:             Exit Function
563:           End If
564:     End If
567:     If Idade_ <= (65) Then
568:          If Pgordura < (19) Then
569:             TabelaGorduraAlvo = 16
570:             Exit Function
572:             ElseIf Pgordura < (21.5) Then
573:             TabelaGorduraAlvo = 20.5
574:             Exit Function
576:             ElseIf Pgordura < (23.5) Then
577:             TabelaGorduraAlvo = 22.5
578:             Exit Function
580:             ElseIf Pgordura < (25.5) Then
581:             TabelaGorduraAlvo = 24.5
582:             Exit Function
584:             ElseIf Pgordura < (27.5) Then
585:             TabelaGorduraAlvo = 24.5
586:             Exit Function
588:             ElseIf Pgordura < (31) Then
589:             TabelaGorduraAlvo = 24.5
590:             Exit Function
592:             ElseIf Pgordura > (31) Then
593:             TabelaGorduraAlvo = 24.5
594:             Exit Function
596:           End If
597:     End If
599: Else
601:     If Idade_ <= (25) Then
602:          If Pgordura < (16.5) Then
603:             TabelaGorduraAlvo = 14.5
604:             Exit Function
606:             ElseIf Pgordura < (19.5) Then
607:             TabelaGorduraAlvo = 18
608:             Exit Function
610:             ElseIf Pgordura < (22.5) Then
611:             TabelaGorduraAlvo = 21
612:             Exit Function
614:             ElseIf Pgordura <= (25.5) Then
615:             TabelaGorduraAlvo = 24
616:             Exit Function
618:             ElseIf Pgordura < (28.5) Then
619:             TabelaGorduraAlvo = 24
620:             Exit Function
622:             ElseIf Pgordura < (32) Then
623:             TabelaGorduraAlvo = 24
624:             Exit Function
626:             ElseIf Pgordura > (32) Then
627:             TabelaGorduraAlvo = 24
628:             Exit Function
630:           End If
631:     End If
634:     If Idade_ <= (35) Then
635:          If Pgordura < (17) Then
636:             TabelaGorduraAlvo = 15
637:             Exit Function
639:             ElseIf Pgordura < (20.5) Then
640:             TabelaGorduraAlvo = 19
641:             Exit Function
643:             ElseIf Pgordura < (23.5) Then
644:             TabelaGorduraAlvo = 22
645:             Exit Function
647:             ElseIf Pgordura < (26) Then
648:             TabelaGorduraAlvo = 24.5
649:             Exit Function
651:             ElseIf Pgordura < (30) Then
652:             TabelaGorduraAlvo = 24.5
653:             Exit Function
655:             ElseIf Pgordura < (34.5) Then
656:             TabelaGorduraAlvo = 24.5
657:             Exit Function
659:             ElseIf Pgordura > (34.5) Then
660:             TabelaGorduraAlvo = 24.5
661:             Exit Function
663:           End If
664:     End If
667:     If Idade_ <= (45) Then
668:          If Pgordura < (19.5) Then
669:             TabelaGorduraAlvo = 17.5
670:             Exit Function
672:             ElseIf Pgordura < (18.5) Then
673:             TabelaGorduraAlvo = 21.5
674:             Exit Function
676:             ElseIf Pgordura < (26.5) Then
677:             TabelaGorduraAlvo = 25
678:             Exit Function
680:             ElseIf Pgordura < (29.5) Then
681:             TabelaGorduraAlvo = 28
682:             Exit Function
684:             ElseIf Pgordura < (32.5) Then
685:             TabelaGorduraAlvo = 28
686:             Exit Function
688:             ElseIf Pgordura < (37) Then
689:             TabelaGorduraAlvo = 28
690:             Exit Function
692:             ElseIf Pgordura > (37) Then
693:             TabelaGorduraAlvo = 28
694:             Exit Function
696:           End If
697:     End If
699:     If Idade_ <= (55) Then
700:          If Pgordura < (22) Then
701:             TabelaGorduraAlvo = 19
702:             Exit Function
704:             ElseIf Pgordura < (25.5) Then
705:             TabelaGorduraAlvo = 24
706:             Exit Function
708:             ElseIf Pgordura < (28.5) Then
709:             TabelaGorduraAlvo = 27
710:             Exit Function
712:             ElseIf Pgordura < (31.5) Then
713:             TabelaGorduraAlvo = 30
714:             Exit Function
716:             ElseIf Pgordura < (34.5) Then
717:             TabelaGorduraAlvo = 30
718:             Exit Function
720:             ElseIf Pgordura < (38.5) Then
721:             TabelaGorduraAlvo = 30
722:             Exit Function
724:             ElseIf Pgordura > (38.5) Then
725:             TabelaGorduraAlvo = 30
726:             Exit Function
728:           End If
729:     End If
732:     If Idade_ <= (65) Then
733:          If Pgordura < (23) Then
734:             TabelaGorduraAlvo = 20
735:             Exit Function
737:             ElseIf Pgordura < (26.5) Then
738:             TabelaGorduraAlvo = 25
739:             Exit Function
741:             ElseIf Pgordura < (29.5) Then
742:             TabelaGorduraAlvo = 28
744:             Exit Function
746:             ElseIf Pgordura < (32.5) Then
747:             TabelaGorduraAlvo = 31
749:             Exit Function
751:             ElseIf Pgordura < (35.5) Then
752:             TabelaGorduraAlvo = 31
754:             Exit Function
756:             ElseIf Pgordura < (38.5) Then
757:             TabelaGorduraAlvo = 31
759:             Exit Function
761:             ElseIf Pgordura > (38.5) Then
762:             TabelaGorduraAlvo = 31
764:             Exit Function
766:           End If
767:     End If
768:     End If
769: End Function
```

## GORDURA.bas — TabelaGorduraOMR_INTERVALO

Origem: `GORDURA.bas:771`.

```text
771: Public Function TabelaGorduraOMR_INTERVALO(Pgordura As Double, sexo_ As String, Idade_ As Double) As String
773: If sexo_ = "Masculino" Then
775:         If Idade_ < (20) Then
776:             TabelaGorduraOMR_INTERVALO = "N/A"
777:             Exit Function
778:         End If
780:         If Idade_ <= (39) Then
781:             TabelaGorduraOMR_INTERVALO = "8 - 19,9"
782:             Exit Function
783:         End If
785:         If Idade_ <= (59) Then
786:             TabelaGorduraOMR_INTERVALO = "11 - 21,9"
787:             Exit Function
788:         End If
790:         If Idade_ <= (79) Then
791:             TabelaGorduraOMR_INTERVALO = "13 - 24,9"
792:             Exit Function
793:         End If
795:         If Idade_ > (79) Then
796:             TabelaGorduraOMR_INTERVALO = "N/A"
797:             Exit Function
798:         End If
800: ElseIf sexo_ = "Feminino" Then
802:         If Idade_ < (20) Then
803:             TabelaGorduraOMR_INTERVALO = "N/A"
804:             Exit Function
805:         End If
807:         If Idade_ <= (39) Then
808:             TabelaGorduraOMR_INTERVALO = "21 - 32,9"
809:             Exit Function
810:         End If
812:         If Idade_ <= (59) Then
813:             TabelaGorduraOMR_INTERVALO = "23 - 33,9"
814:             Exit Function
815:         End If
817:         If Idade_ <= (79) Then
818:             TabelaGorduraOMR_INTERVALO = "24 - 35,9"
819:             Exit Function
820:         End If
822:         If Idade_ > (79) Then
823:             TabelaGorduraOMR_INTERVALO = "N/A"
824:             Exit Function
825:         End If
827: End If
830: End Function
```

## MUSCULOS.bas — TabelaMusculoEsqOMR_INTERVALO

Origem: `MUSCULOS.bas:2`.

```text
2: Public Function TabelaMusculoEsqOMR_INTERVALO(PMuscular As Double, sexo_ As String, Idade_ As Double) As String
4: If sexo_ = "Masculino" Then
6:         If Idade_ < (18) Then
7:             TabelaMusculoEsqOMR_INTERVALO = "N/A"
8:             Exit Function
9:         End If
11:         If Idade_ <= (39) Then
12:             TabelaMusculoEsqOMR_INTERVALO = "33,3 - 39,3"
13:             Exit Function
14:         End If
16:         If Idade_ <= (59) Then
17:             TabelaMusculoEsqOMR_INTERVALO = "33,1 - 39,1"
18:             Exit Function
19:         End If
21:         If Idade_ <= (80) Then
22:             TabelaMusculoEsqOMR_INTERVALO = "32,9 - 38,9"
23:             Exit Function
24:         End If
26:         If Idade_ > (80) Then
27:             TabelaMusculoEsqOMR_INTERVALO = "N/A"
28:             Exit Function
29:         End If
32: ElseIf sexo_ = "Feminino" Then
34:         If Idade_ < (18) Then
35:             TabelaMusculoEsqOMR_INTERVALO = "N/A"
36:             Exit Function
37:         End If
39:         If Idade_ <= (39) Then
40:             TabelaMusculoEsqOMR_INTERVALO = "24,3 - 30,3"
41:             Exit Function
42:         End If
44:         If Idade_ <= (59) Then
45:             TabelaMusculoEsqOMR_INTERVALO = "24,1 - 30,1"
46:             Exit Function
47:         End If
49:         If Idade_ <= (80) Then
50:             TabelaMusculoEsqOMR_INTERVALO = "23,9 - 29,9"
51:             Exit Function
52:         End If
54:         If Idade_ > (80) Then
55:             TabelaMusculoEsqOMR_INTERVALO = "N/A"
56:             Exit Function
57:         End If
59: End If
62: End Function
```

## Rcq.bas — TabelaRCQ

Origem: `Rcq.bas:2`.

```text
2: Public Function TabelaRCQ(sexo_ As String, Idade As Integer, Rcq As Double) As String
5: If sexo_ = "Masculino" Then
7:     If Idade < (20) Then
8:         TabelaRCQ = "N/A"
9:         Exit Function
12:     ElseIf Idade <= (29) Then
13:             If Rcq <= (0.83) Then
14:             TabelaRCQ = "Risco Baixo"
15:             Exit Function
17:             ElseIf Rcq <= (0.88) Then
18:             TabelaRCQ = "Risco Moderado"
19:             Exit Function
21:             ElseIf Rcq <= (0.94) Then
22:             TabelaRCQ = "Risco Alto"
23:             Exit Function
25:             ElseIf Rcq > (0.94) Then
26:             TabelaRCQ = "Muito Alto"
27:             Exit Function
28:             End If
30:     ElseIf Idade <= (39) Then
31:             If Rcq <= (0.84) Then
32:             TabelaRCQ = "Risco Baixo"
33:             Exit Function
35:             ElseIf Rcq <= (0.91) Then
36:             TabelaRCQ = "Risco Moderado"
37:             Exit Function
39:             ElseIf Rcq <= (0.96) Then
40:             TabelaRCQ = "Risco Alto"
41:             Exit Function
43:             ElseIf Rcq > (0.96) Then
44:             TabelaRCQ = "Muito Alto"
45:             Exit Function
46:              End If
49:     ElseIf Idade <= (49) Then
50:             If Rcq <= (0.88) Then
51:             TabelaRCQ = "Risco Baixo"
52:             Exit Function
54:             ElseIf Rcq <= (0.95) Then
55:             TabelaRCQ = "Risco Moderado"
56:             Exit Function
58:             ElseIf Rcq <= (1) Then
59:             TabelaRCQ = "Risco Alto"
60:             Exit Function
62:             ElseIf Rcq > (1) Then
63:             TabelaRCQ = "Muito Alto"
64:             Exit Function
65:             End If
68:     ElseIf Idade <= (59) Then
69:             If Rcq < (0.9) Then
70:             TabelaRCQ = "Risco Baixo"
71:             Exit Function
73:             ElseIf Rcq <= (0.96) Then
74:             TabelaRCQ = "Risco Moderado"
75:             Exit Function
77:             ElseIf Rcq <= (1.02) Then
78:             TabelaRCQ = "Risco Alto"
79:             Exit Function
81:             ElseIf Rcq > (1.02) Then
82:             TabelaRCQ = "Muito Alto"
83:             Exit Function
84:             End If
86:     ElseIf Idade <= (69) Then
87:             If Rcq <= (0.91) Then
88:             TabelaRCQ = "Risco Baixo"
89:             Exit Function
91:             ElseIf Rcq <= (0.98) Then
92:             TabelaRCQ = "Risco Moderado"
93:             Exit Function
95:             ElseIf Rcq <= (1.03) Then
96:             TabelaRCQ = "Risco Alto"
97:             Exit Function
99:             ElseIf Rcq > (1.03) Then
100:             TabelaRCQ = "Muito Alto"
101:             Exit Function
102:             End If
106:     ElseIf Idade > (69) Then
107:             TabelaRCQ = "N/A"
108:             Exit Function
109:     End If
115: Else 'FEMININO
116:     If Idade < (20) Then
117:             TabelaRCQ = "N/A"
118:             Exit Function
121:     ElseIf Idade <= (29) Then
123:             If Rcq <= (0.71) Then
124:             TabelaRCQ = "Risco Baixo"
126:             Exit Function
127:             ElseIf Rcq <= (0.77) Then
128:             TabelaRCQ = "Risco Moderado"
130:             Exit Function
131:             ElseIf Rcq <= (0.82) Then
132:             TabelaRCQ = "Risco Alto"
134:             Exit Function
135:             ElseIf Rcq > (0.82) Then
136:             TabelaRCQ = "Muito Alto"
137:             Exit Function
138:             End If
143:     ElseIf Idade <= (39) Then
144:             If Rcq <= (0.72) Then
145:             TabelaRCQ = "Risco Baixo"
147:             Exit Function
148:             ElseIf Rcq <= (0.78) Then
149:             TabelaRCQ = "Risco Moderado"
151:             Exit Function
152:             ElseIf Rcq <= (0.84) Then
153:             TabelaRCQ = "Risco Alto"
155:             Exit Function
156:             ElseIf Rcq > (0.84) Then
157:             TabelaRCQ = "Muito Alto"
159:             Exit Function
160:             End If
163:     ElseIf Idade <= (49) Then
164:             If Rcq <= (0.73) Then
165:             TabelaRCQ = "Risco Baixo"
167:             Exit Function
168:             ElseIf Rcq <= (0.79) Then
169:             TabelaRCQ = "Risco Moderado"
171:             Exit Function
172:             ElseIf Rcq <= (0.87) Then
173:             TabelaRCQ = "Risco Alto"
175:             Exit Function
176:             ElseIf Rcq > (0.87) Then
177:             TabelaRCQ = "Muito Alto"
178:             Exit Function
179:             End If
183:     ElseIf Idade <= (59) Then
184:             If Rcq <= (0.74) Then
185:             TabelaRCQ = "Risco Baixo"
187:             Exit Function
188:             ElseIf Rcq <= (0.81) Then
189:             TabelaRCQ = "Risco Moderado"
191:             Exit Function
192:             ElseIf Rcq <= (0.88) Then
193:             TabelaRCQ = "Risco Alto"
195:             Exit Function
196:             ElseIf Rcq > (0.88) Then
197:             TabelaRCQ = "Muito Alto"
199:             Exit Function
200:             End If
204:     ElseIf Idade <= (69) Then
205:             If Rcq <= (0.76) Then
206:             TabelaRCQ = "Risco Baixo"
208:             Exit Function
209:             ElseIf Rcq <= (0.83) Then
210:             TabelaRCQ = "Risco Moderado"
212:             Exit Function
213:             ElseIf Rcq <= (0.9) Then
214:             TabelaRCQ = "Risco Alto"
216:             Exit Function
217:             ElseIf Rcq > (0.9) Then
218:             TabelaRCQ = "Muito Alto"
220:             Exit Function
221:             End If
224:         ElseIf Idade > (69) Then
225:             TabelaRCQ = "N/A"
226:             Exit Function
227:         End If
231: End If
233: End Function
```

## Rcq.bas — TabelaRCQ_INTERVALO

Origem: `Rcq.bas:235`.

```text
235: Public Function TabelaRCQ_INTERVALO(sexo_ As String, Idade As Integer, Rcq As Double) As String
238: If sexo_ = "Masculino" Then
240:     If Idade < (20) Then
241:             TabelaRCQ_INTERVALO = "N/A"
242:             Exit Function
245:     ElseIf Idade <= (29) Then
246:             TabelaRCQ_INTERVALO = "0,83 - 0,88"
247:             Exit Function
250:     ElseIf Idade <= (39) Then
251:             TabelaRCQ_INTERVALO = "0,84 - 0,91"
252:             Exit Function
255:     ElseIf Idade <= (49) Then
256:             TabelaRCQ_INTERVALO = "0,88 - 0,95"
257:             Exit Function
260:     ElseIf Idade <= (59) Then
261:             TabelaRCQ_INTERVALO = "0,90 - 0,96"
262:             Exit Function
264:     ElseIf Idade <= (69) Then
265:             TabelaRCQ_INTERVALO = "0,91 - 0,98"
266:             Exit Function
268:     ElseIf Idade > (69) Then
269:             TabelaRCQ_INTERVALO = "N/A"
270:             Exit Function
271:     End If
274: Else 'FEMININO
276:     If Idade < (20) Then
277:             TabelaRCQ_INTERVALO = "N/A"
278:             Exit Function
280:     ElseIf Idade <= (29) Then
281:             TabelaRCQ_INTERVALO = "0,71 - 0,77"
282:             Exit Function
284:     ElseIf Idade <= (39) Then
285:             TabelaRCQ_INTERVALO = "0,72 - 0,78"
286:             Exit Function
288:     ElseIf Idade <= (49) Then
289:             TabelaRCQ_INTERVALO = "0,73 - 0,79"
290:             Exit Function
293:     ElseIf Idade <= (59) Then
294:             TabelaRCQ_INTERVALO = "0,74 - 0,81"
295:             Exit Function
298:     ElseIf Idade <= (69) Then
299:             TabelaRCQ_INTERVALO = "0,76 - 0,83"
300:             Exit Function
302:     ElseIf Idade > (69) Then
303:             TabelaRCQ_INTERVALO = "N/A"
304:             Exit Function
305:     End If
307: End If
309: End Function
```

## Equações_Avaliações.bas — Pollock3

Origem: `Equações_Avaliações.bas:4`.

```text
4: Public Function Pollock3(dTorax As Double, dAbdominal As Double, dCoxa As Double, dTriceps As Double, dSupra As Double, sexo_ As String, Idade_ As Integer) As Double
5: On Error Resume Next
6: Dim SomaD As Double
7: Dim SomaD2 As Double
9:  SomaD = dTorax + dAbdominal + dCoxa
11:  SomaD2 = dTriceps + dSupra + dCoxa
14:                                     Dim PP3 As Double
17:                                         If sexo_ = "Masculino" Then
18:                                         Pollock3 = 1.10938 - 0.0008267 * (SomaD) + 0.0000016 * ((SomaD) ^ 2) - 0.0002574 * Idade_
19:                                         Else
20:                                         Pollock3 = 1.0994921 - 0.0009929 * (SomaD2) + 0.0000023 * ((SomaD2) ^ 2) - 0.0001392 * Idade_
21:                                         End If
24: End Function
```

## Equações_Avaliações.bas — Pollock7

Origem: `Equações_Avaliações.bas:25`.

```text
25: Public Function Pollock7(dAxilar As Double, dSubescapular As Double, dTorax As Double, dAbdominal As Double, dCoxa As Double, dTriceps As Double, dSupra As Double, sexo_ As String, Idade_ As Integer) As Double
26: On Error Resume Next
27: Dim SomaD As Double
28:  SomaD = dTorax + dAxilar + dTriceps + dSubescapular + dAbdominal + dSupra + dCoxa
30:                                         If sexo_ = "Masculino" Then
31:                                         Pollock7 = 1.112 - 0.00043499 * SomaD + 0.00000055 * (SomaD ^ 2) - 0.00028826 * Idade_
33:                                         Else
34:                                         Pollock7 = 1.097 - 0.00046971 * SomaD + 0.00000056 * (SomaD ^ 2) - 0.00012828 * Idade_
35:                                         End If
38: End Function
```

## Equações_Avaliações.bas — Slaughter

Origem: `Equações_Avaliações.bas:40`.

```text
40: Public Function Slaughter(dTriceps As Double, dSubescapular As Double, sexo_ As String, fPuberal As String, etnia_) As Double
41: On Error Resume Next
42: Dim SomaD As Double
43:  SomaD = dTriceps + dSubescapular
45:  Dim VarPub As Double
61: If SomaD <= 35 Then
63: If sexo_ = "Masculino" Then
66: If etnia_ = "Branco" And fPuberal = "Pré-Púbere" Then
67: VarPub = 1.7
68: Slaughter = Round(((1.21 * (SomaD)) - (0.008 * (SomaD) ^ 2)) - VarPub, 2)
70: End If
72: If etnia_ = "Branco" And fPuberal = "Púbere" Then
73: VarPub = 3.4
74: Slaughter = Round(((1.21 * (SomaD)) - (0.008 * (SomaD) ^ 2)) - VarPub, 2)
76: End If
78: If etnia_ = "Branco" And fPuberal = "Pós-Púbere" Then
79: VarPub = 5.5
80: Slaughter = Round(((1.21 * (SomaD)) - (0.008 * (SomaD) ^ 2)) - VarPub, 2)
82: End If
86: If etnia_ = "Negro" And fPuberal = "Pré-Púbere" Then
87: VarPub = 3.2
88: Slaughter = Round(((1.21 * (SomaD)) - (0.008 * (SomaD) ^ 2)) - VarPub, 2)
90: End If
92: If etnia_ = "Negro" And fPuberal = "Púbere" Then
93: VarPub = 5.2
94: Slaughter = Round(((1.21 * (SomaD)) - (0.008 * (SomaD) ^ 2)) - VarPub, 2)
96: End If
98: If etnia_ = "Negro" And fPuberal = "Pós-Púbere" Then
99: VarPub = 6.8
100: Slaughter = Round(((1.21 * (SomaD)) - (0.008 * (SomaD) ^ 2)) - VarPub, 2)
102: End If
104: Else
107: VarPub = 2.5
108: Slaughter = Round(((1.33 * (SomaD)) - (0.013 * (SomaD) ^ 2)) - VarPub, 2)
111: End If
113: Else
115: If sexo_ And SomaD > 35 Then
117: VarPub = 1.6
118: Slaughter = Round((0.783 * (SomaD)) + VarPub, 2)
121: Else
123: VarPub = 9.7
124: Slaughter = Round((0.546 * (SomaD)) + VarPub, 2)
126: End If
127: End If
128: End Function
```

## Equações_Avaliações.bas — Weltman

Origem: `Equações_Avaliações.bas:132`.

```text
132: Public Function Weltman(cAbodomen As Double, cCintura As Double, altura_ As Double, peso_ As Double, sexo_ As String) As Double
133: On Error Resume Next
136: If sexo_ = "Masculino" Then
137:    Weltman = Round((0.31457 * ((cAbodomen + cCintura) / 2)) - 0.10969 * (peso_) + 10.8336, 2)
138: Else
140: Weltman = Round((0.11077 * ((cAbodomen + cCintura) / 2)) - 0.17666 * (altura_) + (0.14354 * (peso_)) + 51.03301, 2)
141: End If
143: End Function
```

## Equações_Avaliações.bas — Petroski

Origem: `Equações_Avaliações.bas:147`.

```text
147: Public Function Petroski(dSubscapular As Double, dTriceps As Double, dSupra As Double, dPaturrilha As Double, dAxilar As Double, dCoxa As Double, sexo_ As String, Idade_ As Double) As Double
148: On Error Resume Next
149: Dim SomaD As Double
150: Dim SomaD2 As Double
152: SomaD = dSubscapular + dTriceps + dSupra + dPaturrilha
154: SomaD2 = dAxilar + dSupra + dCoxa + dPaturrilha
162:                                         If sexo_ = "Masculino" Then
163:                                         Petroski = 1.10726863 - 0.00081201 * (SomaD) + 0.00000212 * ((SomaD) ^ 2) - 0.00041761 * Idade_
164:                                         Else
168:                                         Petroski = 1.1954713 - 0.07513507 * (Application.WorksheetFunction.Log10(SomaD2)) - 0.00041072 * Idade_
169:                                         End If
172: End Function
```

## Equações_Avaliações.bas — Visser

Origem: `Equações_Avaliações.bas:174`.

```text
174: Public Function Visser(dBiceps As Double, dTriceps As Double, sexo_) As Double
175: On Error Resume Next
184: Dim SomaD As Double
185: Dim log As Double
188:  SomaD = dBiceps + dTriceps
190: log = Application.WorksheetFunction.Log10(SomaD)
192:                                         If sexo_ = "Masculino" Then
193:                                         Visser = 0.0186 * (1) - 0.03 * log + 1.0481
194:                                         Else
195:                                         Visser = 0.0186 * (0) - 0.03 * log + 1.0481
196:                                         End If
198: End Function
```

## Equações_Avaliações.bas — Guedes

Origem: `Equações_Avaliações.bas:199`.

```text
199: Public Function Guedes(sexo_ As String, dSupra As Double, dCoxa As Double, dSubescapular As Double, dAbdominal, dTriceps) As Double
200: On Error Resume Next
209: Dim SomaDM As Double
210: Dim SomaDH As Double
211: Dim DENS As Double
212: Dim log As Double
213: Dim log2 As Double
216:  SomaDM = dSupra + dCoxa + dSubescapular
219: SomaDH = dAbdominal + dTriceps + dSupra
221: log = Application.WorksheetFunction.Log10(SomaDM)
222: log2 = Application.WorksheetFunction.Log10(SomaDH)
224:                                         If sexo_ = "Feminino" Then
225:                                         Guedes = 1.1665 - 0.07063 * log
226:                                         Else
228:                                         Guedes = 1.17136 - 0.06706 * log2
229:                                         End If
232: End Function
```

## Equações_Avaliações.bas — Log10

Origem: `Equações_Avaliações.bas:233`.

```text
233: Public Function Log10(X)
234:     Log10 = log(X) / log(10#)
235: End Function
```

## Equações_Avaliações.bas — Chumlea88P

Origem: `Equações_Avaliações.bas:237`.

```text
237: Public Function Chumlea88P(Sexo As String, Cp As Double, AJ As Double, CB As Double, DCSE As Double) As Double
238: On Error Resume Next
239: If Sexo = "Feminino" Then
242: Chumlea88P = Round((1.27 * Cp) + (0.87 * AJ) + (0.98 * CB) + (0.4 * DCSE) - 62.35, 2)
243: Else
245: Chumlea88P = Round((0.98 * Cp) + (1.16 * AJ) + (1.73 * CB) + (0.37 * DCSE) - 81.69, 2)
246: End If
248: End Function
```

## Equações_Avaliações.bas — Chumlea85A

Origem: `Equações_Avaliações.bas:251`.

```text
251: Public Function Chumlea85A(Sexo As String, Idade As Double, alturajoelho As Double) As Double
252: On Error Resume Next
256: If Sexo = "Feminino" Then
258: Chumlea85A = Round(84.88 - (0.24 * Idade) + (1.83 * alturajoelho), 2)
259: Else
261: Chumlea85A = Round(64.19 - (0.04 * Idade) + (2.02 * alturajoelho), 2) 'OK
263: End If
265: End Function
```

## Equações_Avaliações.bas — Osterkamp95P

Origem: `Equações_Avaliações.bas:267`.

```text
267: Public Function Osterkamp95P(peso As Double, porcentagem As Double)
268: On Error Resume Next
280: Osterkamp95P = Round((peso) / (100 - porcentagem) * 100, 2)
282: End Function
```

## Equações_Avaliações.bas — PesoOsseo

Origem: `Equações_Avaliações.bas:285`.

```text
285: Public Function PesoOsseo(AlturaCm As Double, Punho As Double, Femur As Double) As Double
286: Dim PO As Double
288: PO = 3.02 * ((((AlturaCm / 100) ^ 2) * (Punho / 1000) * (Femur / 1000) * (400)) ^ 0.712)
289: PesoOsseo = Round(PO, 2)
290: End Function
```

## Equações_Avaliações.bas — PesoMuscular

Origem: `Equações_Avaliações.bas:292`.

```text
292: Public Function PesoMuscular(PesoAtual_ As Double, PesoGordo_ As Double, PesoOsseo_ As Double, PesoResidual_ As Double) As Double
294:   PesoMuscular = PesoAtual_ - (PesoGordo_ + PesoOsseo_ + PesoResidual_)
295: End Function
```

## Equações_Avaliações.bas — PesoResidual

Origem: `Equações_Avaliações.bas:297`.

```text
297: Public Function PesoResidual(sexo_ As String, peso_ As Double) As Double
299:  If sexo_ = "Masculino" Then
301:  PesoResidual = Round(peso_ * 0.241, 2)
303:  Else
304:  PesoResidual = Round(peso_ * 0.209, 2)
305:  End If
307: End Function
```

## FrmCalcPeso.frm — BtnCalcular_Click

Origem: `FrmCalcPeso.frm:10`.

```text
10: Private Sub BtnCalcular_Click()
11: On Error Resume Next
12: Dim PesoGordo As Double
13: Dim PesoAlvo As Double
14: Dim PesoMagroAtual
15: Dim GorduraAlvo As Double
16: Dim PI As Double
17: Dim PI2 As Double
19: If OptionButton1.Value = False And OptionButton2.Value = False Then
20: MsgBox "Selecione uma opção!"
21: OptionButton1.BackColor = vbRed
22: OptionButton1.SetFocus
23: Exit Sub
24: End If
26: If OptionButton1.Value = True Then
28: If TxtGorduraAtual = "" Or TxtGorduraAtual = 0 Then
29: MsgBox "Insira a gordura atual para prosseguir!"
30: TxtGorduraAtual.BackColor = vbRed
31: TxtGorduraAtual.SetFocus
32: Exit Sub
33: End If
35: If TxtGorduraAlvo = "" Or TxtGorduraAlvo = 0 Then
36: MsgBox "Insira a gordura alvo para prosseguir!"
37: TxtGorduraAlvo.BackColor = vbRed
38: TxtGorduraAlvo.SetFocus
39: Exit Sub
40: End If
42: PesoMagroAtual = CDbl(FrmCalcPeso.TxtPesoAtual) - (CDbl(FrmCalcPeso.TxtPesoAtual) * (CDbl(FrmCalcPeso.TxtGorduraAtual) / 100))
43: PesoAlvo = (PesoMagroAtual * GorduraAlvo) / (1 - GorduraAlvo) + PesoMagroAtual
44: PI = (CDbl(FrmCalcPeso.TxtGorduraAlvo) / 100) * PesoMagroAtual / (1 - (CDbl(FrmCalcPeso.TxtGorduraAlvo) / 100)) + PesoMagroAtual
45: FrmCalcPeso.TxtPesoAlvo = Format(PI, "#,##0.00")
46: FrmCalcPeso.TxtControlePeso = Format(CDbl(FrmCalcPeso.TxtPesoAlvo) - CDbl(FrmCalcPeso.TxtPesoAtual), "#,##0.00")
49: ElseIf OptionButton2.Value = True Then
51: If TxtImcAtual = "" Or TxtImcAtual = 0 Then
52: MsgBox "Calcule o IMC atual para prosseguir!"
53: TxtImcAtual.BackColor = vbRed
54: TxtImcAtual.SetFocus
55: Exit Sub
56: End If
58: If TxtImcAlvo = "" Or TxtImcAlvo = 0 Then
59: MsgBox "Insira o IMC alvo para prosseguir!"
60: TxtImcAlvo.BackColor = vbRed
61: TxtImcAlvo.SetFocus
62: Exit Sub
63: End If
68: PI = CDbl(TxtImcAlvo.Value) * ((CDbl(FrmAvaliacaoB.TxtAltura.Value) / 100) ^ 2)
70: FrmCalcPeso.TxtPesoAlvo = Format(PI, "#,##0.00")
71: FrmCalcPeso.TxtControlePeso = Format(CDbl(FrmCalcPeso.TxtPesoAlvo) - CDbl(FrmCalcPeso.TxtPesoAtual), "#,##0.00")
73: End If
75: End Sub
```

## FrmBioimpC.frm — RecalcComposicao

Origem: `FrmBioimpC.frm:12`.

```text
12: Private Sub RecalcComposicao()
13:     Dim p As Double
14:     If Not IsNumeric(TxtPeso) Then Exit Sub
15:     p = CDbl(TxtPeso)
16:     If p <= 0 Then Exit Sub
18:     TxtPesoPerc = "100,00"
19:     If IsNumeric(TxtGorduraAtualKg) Then TxtPGorduraAtual = Format(CDbl(TxtGorduraAtualKg) / p * 100, "#,##0.00")
20:     If IsNumeric(TxtMassaOssea) Then TxtMassaOsseaPerc = Format(CDbl(TxtMassaOssea) / p * 100, "#,##0.00")
21:     If IsNumeric(TxtProteinaKg) Then TxtProteina = Format(CDbl(TxtProteinaKg) / p * 100, "#,##0.00")
22:     If IsNumeric(TxtAguaCorporalKg) Then TxtAguaCorporal = Format(CDbl(TxtAguaCorporalKg) / p * 100, "#,##0.00")
23:     If IsNumeric(TxtMassaMuscular) Then TxtMassaMuscularPerc = Format(CDbl(TxtMassaMuscular) / p * 100, "#,##0.00")
24:     If IsNumeric(TxtPesoMuscularEsq) Then TxtMassaEsqueletica = Format(CDbl(TxtPesoMuscularEsq) / p * 100, "#,##0.00")
25: End Sub
```

## FrmBioimpC.frm — CommandButton179_Click

Origem: `FrmBioimpC.frm:27`.

```text
27: Private Sub CommandButton179_Click()
28: On Error Resume Next
30:  FrmAvaliacaoC.TxtImc = Format(TxtImc, "#,##0.00")
31:  FrmAvaliacaoC.TxtPGorduraAtual = Format(TxtPGorduraAtual, "#,##0.00")
32:  FrmAvaliacaoC.TxtPesoGordo = Format(TxtGorduraAtualKg, "#,##0.00")   ' kg EXATO digitado -> col32
33:  FrmAvaliacaoC.TxtPorcentagemMuscEsqueceletico = Format(TxtMassaEsqueletica, "#,##0.00")
34:  FrmAvaliacaoC.TxtNivelGorduraVisceral = Format(TxtGorduraVisceral, "#,##0.00")
35:  FrmAvaliacaoC.TxtPesoMuscularEsq = Format(TxtPesoMuscularEsq, "#,##0.00")
36:  FrmAvaliacaoC.TxtPontuacao = Format(TxtPontuacao, "#,##0")
38: TxtZ20BracoD = TxtZ20BracoD1
39: FrmAvaliacaoC.TxtZ20BracoD = Format(TxtZ20BracoD1, "#,##0")
41: FrmAvaliacaoC.TxtZ20BracoE = Format(TxtZ20BracoE, "#,##0")
42: FrmAvaliacaoC.TxtZ20Tronco = Format(TxtZ20Tronco, "#,##0")
43: FrmAvaliacaoC.TxtZ20PernaD = Format(TxtZ20PernaD, "#,##0")
44: FrmAvaliacaoC.TxtZ20PernaE = Format(TxtZ20PernaE, "#,##0")
45: TxtZ100BracoD = TxtZ100BracoD1
46: FrmAvaliacaoC.TxtZ100BracoD = Format(TxtZ100BracoD1, "#,##0")
48: FrmAvaliacaoC.TxtZ100BracoE = Format(TxtZ100BracoE, "#,##0")
49: FrmAvaliacaoC.TxtZ100Tronco = Format(TxtZ100Tronco, "#,##0")
50: FrmAvaliacaoC.TxtZ100PernaD = Format(TxtZ100PernaD, "#,##0")
51: FrmAvaliacaoC.TxtZ100PernaE = Format(TxtZ100PernaE, "#,##0")
53: FrmAvaliacaoC.TxtMuscBracoDirPct = Format(TxtMuscBracoDirPct, "#,##0.00")
54: FrmAvaliacaoC.TxtMuscBracoDirKg = Format(TxtMuscBracoDirKg, "#,##0.00")
55: FrmAvaliacaoC.TxtMuscBracoEsqPct = Format(TxtMuscBracoEsqPct, "#,##0.00")
56: FrmAvaliacaoC.TxtMuscBracoEsqKg = Format(TxtMuscBracoEsqKg, "#,##0.00")
57: FrmAvaliacaoC.TxtMuscTroncoPct = Format(TxtMuscTroncoPct, "#,##0.00")
58: FrmAvaliacaoC.TxtMuscTroncoKg = Format(TxtMuscTroncoKg, "#,##0.00")
59: FrmAvaliacaoC.TxtMuscPernaDirPct = Format(TxtMuscPernaDirPct, "#,##0.00")
60: FrmAvaliacaoC.TxtMuscPernaDirKg = Format(TxtMuscPernaDirKg, "#,##0.00")
61: FrmAvaliacaoC.TxtMuscPernaEsqPct = Format(TxtMuscPernaEsqPct, "#,##0.00")
62: FrmAvaliacaoC.TxtMuscPernaEsqKg = Format(TxtMuscPernaEsqKg, "#,##0.00")
64:  FrmAvaliacaoC.TxtIdadeCorpo = Format(TxtIdadeCorpo, "#,##0.00")
66:   If OptionButton44.Value = True Then
67: FrmAvaliacaoC.TxtIdadeCorporalClassificacao = OptionButton44.Caption
68: ElseIf OptionButton45.Value = True Then
69: FrmAvaliacaoC.TxtIdadeCorporalClassificacao = OptionButton45.Caption
70: End If
72:  FrmAvaliacaoC.TxtTmb = Format(TxtTmb, "#,##0.00")
74:   If OptionButton42.Value = True Then
75: FrmAvaliacaoC.TxtTmbClassificacao = OptionButton42.Caption
76: ElseIf OptionButton43.Value = True Then
77: FrmAvaliacaoC.TxtTmbClassificacao = OptionButton43.Caption
78: End If
80:  If OptionButton13.Value = True Then
81: FrmAvaliacaoC.TxtImcClassificacao = OptionButton13.Caption
82: ElseIf OptionButton14.Value = True Then
83: FrmAvaliacaoC.TxtImcClassificacao = OptionButton14.Caption
84: ElseIf OptionButton15.Value = True Then
85: FrmAvaliacaoC.TxtImcClassificacao = OptionButton15.Caption
86: ElseIf OptionButton16.Value = True Then
87: FrmAvaliacaoC.TxtImcClassificacao = OptionButton16.Caption
88: End If
91: If OptionButton1.Value = True Then
92: FrmAvaliacaoC.TxtClassificacao = OptionButton1.Caption
93: ElseIf OptionButton2.Value = True Then
94: FrmAvaliacaoC.TxtClassificacao = OptionButton2.Caption
95: ElseIf OptionButton3.Value = True Then
96: FrmAvaliacaoC.TxtClassificacao = OptionButton3.Caption
97: ElseIf OptionButton4.Value = True Then
98: FrmAvaliacaoC.TxtClassificacao = OptionButton4.Caption
99: End If
102: If OptionButton5.Value = True Then
103: FrmAvaliacaoC.TxtPorcentagemMuscEsqueceleticoCLASSIFICACAO = OptionButton5.Caption
104: ElseIf OptionButton6.Value = True Then
105: FrmAvaliacaoC.TxtPorcentagemMuscEsqueceleticoCLASSIFICACAO = OptionButton6.Caption
106: ElseIf OptionButton8.Value = True Then
107: FrmAvaliacaoC.TxtPorcentagemMuscEsqueceleticoCLASSIFICACAO = OptionButton8.Caption
108: End If
111: If OptionButton9.Value = True Then
112: FrmAvaliacaoC.TxtNivelGorduraVisceralCLASSIFICACAO = OptionButton9.Caption
113: ElseIf OptionButton10.Value = True Then
114: FrmAvaliacaoC.TxtNivelGorduraVisceralCLASSIFICACAO = OptionButton10.Caption
115: ElseIf OptionButton12.Value = True Then
116: FrmAvaliacaoC.TxtNivelGorduraVisceralCLASSIFICACAO = OptionButton12.Caption
117: End If
119: FrmAvaliacaoC.TxtPeso = Format(TxtPeso, "#,##0.00")
120: If OptionButton29.Value = True Then
121: FrmAvaliacaoC.TxtPesoClassificacao = OptionButton29.Caption
122: ElseIf OptionButton30.Value = True Then
123: FrmAvaliacaoC.TxtPesoClassificacao = OptionButton30.Caption
124: ElseIf OptionButton31.Value = True Then
125: FrmAvaliacaoC.TxtPesoClassificacao = OptionButton31.Caption
126: ElseIf OptionButton32.Value = True Then
127: FrmAvaliacaoC.TxtPesoClassificacao = OptionButton32.Caption
128: End If
130: FrmAvaliacaoC.TxtMassaLivreGordura = Format(TxtMlg, "#,##0.00")
133: FrmAvaliacaoC.TxtMassaOssea = Format(TxtMassaOssea, "#,##0.00")
134: FrmAvaliacaoC.TextBox50 = Format(TxtMassaOsseaPerc, "#,##0.00")
135: If OptionButton36.Value = True Then
136: FrmAvaliacaoC.TxtMassaOsseaClassificacao = OptionButton36.Caption
137: ElseIf OptionButton37.Value = True Then
138: FrmAvaliacaoC.TxtMassaOsseaClassificacao = OptionButton37.Caption
139: ElseIf OptionButton38.Value = True Then
140: FrmAvaliacaoC.TxtMassaOsseaClassificacao = OptionButton38.Caption
141: ElseIf Opt3Alto.Value = True Then
142: FrmAvaliacaoC.TxtMassaOsseaClassificacao = Opt3Alto.Caption
143: End If
146: FrmAvaliacaoC.TxtTaxaMuscular = Format(TxtMassaMuscularPerc, "#,##0.00")
147: If OptionButton17.Value = True Then
148: FrmAvaliacaoC.TxtTaxaMuscularClassificacao = OptionButton17.Caption
149: ElseIf OptionButton18.Value = True Then
150: FrmAvaliacaoC.TxtTaxaMuscularClassificacao = OptionButton18.Caption
151: ElseIf OptionButton20.Value = True Then
152: FrmAvaliacaoC.TxtTaxaMuscularClassificacao = OptionButton20.Caption
153: End If
155: FrmAvaliacaoC.TxtGorduraSubcutanea = Format(TxtGorduraSubcutanea, "#,##0.00")
156: If OptionButton33.Value = True Then
157: FrmAvaliacaoC.TxtGorduraSubcutaneaClassificacao = OptionButton33.Caption
158: ElseIf OptionButton34.Value = True Then
159: FrmAvaliacaoC.TxtGorduraSubcutaneaClassificacao = OptionButton34.Caption
160: ElseIf OptionButton35.Value = True Then
161: FrmAvaliacaoC.TxtGorduraSubcutaneaClassificacao = OptionButton35.Caption
162: End If
164: FrmAvaliacaoC.TxtAguaCorporal = Format(TxtAguaCorporal, "#,##0.00")
165: If OptionButton22.Value = True Then
166: FrmAvaliacaoC.TxtAguaCorporalClassificacao = OptionButton22.Caption
167: ElseIf OptionButton23.Value = True Then
168: FrmAvaliacaoC.TxtAguaCorporalClassificacao = OptionButton23.Caption
169: ElseIf OptionButton24.Value = True Then
170: FrmAvaliacaoC.TxtAguaCorporalClassificacao = OptionButton24.Caption
171: End If
173: FrmAvaliacaoC.TxtMassaMuscular = Format(TxtMassaMuscular, "#,##0.00")
174: If OptionButton25.Value = True Then
175: FrmAvaliacaoC.TxtMassaMuscularClassificacao = OptionButton25.Caption
176: ElseIf OptionButton26.Value = True Then
177: FrmAvaliacaoC.TxtMassaMuscularClassificacao = OptionButton26.Caption
178: ElseIf OptionButton28.Value = True Then
179: FrmAvaliacaoC.TxtMassaMuscularClassificacao = OptionButton28.Caption
180: End If
182: FrmAvaliacaoC.TxtProteina = Format(TxtProteina, "#,##0.00")
183: If OptionButton39.Value = True Then
184: FrmAvaliacaoC.TxtProteinaClassificacao = OptionButton39.Caption
185: ElseIf OptionButton40.Value = True Then
186: FrmAvaliacaoC.TxtProteinaClassificacao = OptionButton40.Caption
187: ElseIf OptionButton41.Value = True Then
188: FrmAvaliacaoC.TxtProteinaClassificacao = OptionButton41.Caption
189: End If
191:  FrmAvaliacaoC.TxtPorcentagemBracoDireito = Format(TxtPorcentagemBracoDireito, "#,##0.00")
192:  FrmAvaliacaoC.TxtPesoBracoDireito = Format(TxtPesoBracoDireito, "#,##0.00")
193:  FrmAvaliacaoC.TxtPorcentagemBracoEsquerdo = Format(TxtPorcentagemBracoEsquerdo, "#,##0.00")
194:  FrmAvaliacaoC.TxtPesoBracoEsquerdo = Format(TxtPesoBracoEsquerdo, "#,##0.00")
195:  FrmAvaliacaoC.TxtPorcentagemAbdominal = Format(TxtPorcentagemAbdominal, "#,##0.00")
196:  FrmAvaliacaoC.TxtPesoAbdominal = Format(TxtPesoAbdominal, "#,##0.00")
197:  FrmAvaliacaoC.TxtPorcentagemPernaDireita = Format(TxtPorcentagemPernaDireita, "#,##0.00")
198:  FrmAvaliacaoC.TxtPesoPernaDireita = Format(TxtPesoPernaDireita, "#,##0.00")
199:  FrmAvaliacaoC.TxtPorcentagemPernaEsquerda = Format(TxtPorcentagemPernaEsquerda, "#,##0.00")
200:  FrmAvaliacaoC.TxtPesoPernaEsquerda = Format(TxtPesoPernaEsquerda, "#,##0.00")
202: FrmAvaliacaoC.Image57.Visible = True ' protocolo
203: FrmAvaliacaoC.Image59.Visible = True ' peso total classif
204: FrmAvaliacaoC.CheckImc.Visible = True
205: FrmAvaliacaoC.CheckGordura.Visible = True
206: FrmAvaliacaoC.Image60.Visible = True 'taxa musc
207: FrmAvaliacaoC.Image61.Visible = True 'mlg
208: FrmAvaliacaoC.Image62.Visible = True 'Subcutanea
209: FrmAvaliacaoC.CheckMuscEsqueletico.Visible = True
210: FrmAvaliacaoC.Image63.Visible = True ' agua
211: FrmAvaliacaoC.CheckGordVisceral.Visible = True
212: FrmAvaliacaoC.Image65.Visible = True ' massa muscular
213: FrmAvaliacaoC.Image64.Visible = True ' osso
214: FrmAvaliacaoC.Image66.Visible = True 'proteina
215: FrmAvaliacaoC.CheckTMB.Visible = True
216: FrmAvaliacaoC.Image58.Visible = True 'idade corpo
218:  Unload Me
219:  MsgBox "Valores Aplicados!"
220: End Sub
```

## FrmAvaliacaoA.frm — CommandButton114_Click

Origem: `FrmAvaliacaoA.frm:44`.

```text
44: Private Sub CommandButton114_Click()
45: On Error Resume Next
50: If TxtCintura.Value = 0 Then
51: MsgBox "Informe a Circinferência da Cintura"
52: TxtCintura.BackColor = vbRed
53: TxtCintura.SetFocus
54: Exit Sub
55: End If
57: If TxtQuadril.Value = 0 Then
58: MsgBox "Informe a Circinferência do Quadril"
59: TxtQuadril.BackColor = vbRed
60: TxtQuadril.SetFocus
61: Exit Sub
62: End If
65: TxtRcq = Format(CDbl(FrmAvaliacaoA.TxtCintura) / CDbl(FrmAvaliacaoA.TxtQuadril), "#,##0.00")
66: TxtRcqDesc = TabelaRCQ(TxtSexo, TxtIdade, TxtRcq)
67: CheckRcq.Visible = True
72: If CInt(TxtIdade) > 19 And CInt(TxtIdade) < 60 Then
75: If TxtPunho = 0 Or TxtFemur = 0 Then
78: TxtPesoOsseo = Format(0, "#,##0.00")
79: TextBox45 = Format(0, "#,##0.00")
82: TxtPesoMuscular = Format(0, "#,##0.00")
83: TextBox43 = Format(0, "#,##0.00")
86:  TxtPesoResidualKg = Format(0, "#,##0.00")
87: TextBox79 = Format(0, "#,##0.00")
89: ElseIf TxtPunho <> 0 And TxtFemur <> 0 Then
91:  TxtPesoResidualKg = Format(PesoResidual(TxtSexo, CDbl(TxtPesoKg)), "#,##0.00")
92:  TextBox79 = Format((CDbl(TxtPesoResidualKg) / CDbl(TxtPesoKg) * 100), "#,##0.00")
93:  CheckPesoResidual.Visible = True
97: TxtPesoOsseo = PesoOsseo(FrmAvaliacaoA.TxtAltura, FrmAvaliacaoA.TxtPunho, FrmAvaliacaoA.TxtFemur)
98: TextBox45 = Format((CDbl(TxtPesoOsseo) / CDbl(TxtPesoKg) * 100), "#,##0.00")
99: CheckPesoOsseo.Visible = True
102: TxtPesoMuscular = Format(PesoMuscular(CDbl(TxtPesoKg), CDbl(TxtPesoGordo), CDbl(TxtPesoOsseo), CDbl(TxtPesoResidualKg)), "#,##0.00")
103: TextBox43 = Format((CDbl(TxtPesoMuscular) / CDbl(TxtPesoKg) * 100), "#,##0.00") '%
104: CheckPesoMuscular.Visible = True
105: End If
107: Else
108:  TxtPesoResidualKg = Format(0, "#,##0.00")
109:  TextBox79 = Format(0, "#,##0.00")
110:  CheckPesoResidual.Visible = True
113: TxtPesoOsseo = Format(0, "#,##0.00")
114: TextBox45 = Format(0, "#,##0.00")
115: CheckPesoOsseo.Visible = True
118: TxtPesoMuscular = Format(0, "#,##0.00")
119: TextBox43 = Format(0, "#,##0.00")
120: CheckPesoMuscular.Visible = True
121: End If
124: End Sub
```

## FrmAvaliacaoA.frm — CommandButton207_Click

Origem: `FrmAvaliacaoA.frm:260`.

```text
260: Private Sub CommandButton207_Click()
261: On Error Resume Next
263: Idade = TxtIdade
265: If TxtPesoKg = 0 Or TxtPesoKg = "" Then
266: MsgBox "Insira o Peso em Kg para Prosseguir"
267: TxtPesoKg.BackColor = vbRed
268: TxtPesoKg.SetFocus
269: Exit Sub
270: End If
272: If TxtAltura = 0 Or TxtAltura = "" Then
273: MsgBox "Insira a altura em Cm para Prosseguir"
274: TxtAltura.BackColor = vbRed
275: TxtAltura.SetFocus
276: Exit Sub
277: End If
280: TxtPesoTotal = Format(TxtPesoKg, "#,##0.00")
281: TextBox46 = Format(100, "#,##0.00")
282: CheckPesoTotal.Visible = True
287: If Idade < 5 Then
289: TxtImc = Format(0, "#,##0.00")
290: TxtImcClassificacao = "N/A"
291: CheckImc.Visible = True
293: ElseIf Idade < 20 Then
295: TxtImc = Format(CDbl(FrmAvaliacaoA.TxtPesoKg) / ((CDbl(FrmAvaliacaoA.TxtAltura) / 100) ^ 2), "#,##0.00")
296: TxtImcClassificacao = ImcDesc
297: CheckImc.Visible = True
299: ElseIf Idade < 60 Then
301: TxtImc = Format(CDbl(FrmAvaliacaoA.TxtPesoKg) / ((CDbl(FrmAvaliacaoA.TxtAltura) / 100) ^ 2), "#,##0.00")
302: TxtImcClassificacao = TabelaIMC(CDbl(TxtImc))
303: CheckImc.Visible = True
305: ElseIf Idade > 59 Then
307: TxtImc = Format(CDbl(FrmAvaliacaoA.TxtPesoKg) / ((CDbl(FrmAvaliacaoA.TxtAltura) / 100) ^ 2), "#,##0.00")
308: TxtImcClassificacao = IMCidoso(CDbl(TxtImc))
309: CheckImc.Visible = True
311: End If
312: End Sub
```

## FrmAvaliacaoA.frm — TxtPGorduraAtual_Change

Origem: `FrmAvaliacaoA.frm:749`.

```text
749: Private Sub TxtPGorduraAtual_Change()
750: On Error Resume Next
752: If TxtPGorduraAtual <> 0 And TxtPGorduraAtual <> "" Then
754:  TxtPesoGordo = Format((CDbl(FrmAvaliacaoA.TxtPGorduraAtual) / 100) * CDbl(FrmAvaliacaoA.TxtPesoKg), "#,##0.00")
755:  TxtClassificacao = TabelaGordura(TxtPGorduraAtual, TxtSexo, TxtIdade)
756:  TextBox44 = Format((CDbl(TxtPesoGordo) / CDbl(FrmAvaliacaoA.TxtPesoKg) * 100), "#,##0.00")
757:  CheckPesoGordo.Visible = True
758:  CheckGordura.Visible = True
762: TxtMlg = Format(CDbl(FrmAvaliacaoA.TxtPesoKg) - CDbl(TxtPesoGordo), "#,##0.00")
763: TextBox80 = Format((CDbl(TxtMlg) / CDbl(FrmAvaliacaoA.TxtPesoKg) * 100), "#,##0.00")
764: CheckMassaLivreGordura.Visible = True
766: End If
767: End Sub
```

## FrmAvaliacaoB.frm — TxtPGorduraAtual_Change

Origem: `FrmAvaliacaoB.frm:642`.

```text
642: Private Sub TxtPGorduraAtual_Change()
643: On Error Resume Next
645: If TxtPGorduraAtual <> 0 And TxtPGorduraAtual <> "" Then
647:  TxtPesoGordo = Format((CDbl(FrmAvaliacaoB.TxtPGorduraAtual) / 100) * CDbl(FrmAvaliacaoB.TxtPesoKg), "#,##0.00")
648:  TextBox44 = Format((CDbl(TxtPesoGordo) / CDbl(FrmAvaliacaoB.TxtPesoKg) * 100), "#,##0.00")
649:  CheckPesoGordo.Visible = True
652: TxtMlg = Format(CDbl(FrmAvaliacaoB.TxtPesoKg) - CDbl(TxtPesoGordo), "#,##0.00")
653:  TextBox43 = Format((CDbl(TxtMlg) / CDbl(FrmAvaliacaoB.TxtPesoKg) * 100), "#,##0.00")
654: CheckMassaLivreGordura.Visible = True
658:  TxtIntevaloGordura = TabelaGorduraOMR_INTERVALO(TxtPGorduraAtual, TxtSexo, TxtIdade)
659:  End If
660: End Sub
```

## FrmAvaliacaoB.frm — TxtPorcentagemMuscEsqueceletico_Change

Origem: `FrmAvaliacaoB.frm:677`.

```text
677: Private Sub TxtPorcentagemMuscEsqueceletico_Change()
678: On Error Resume Next
680: If TxtPorcentagemMuscEsqueceletico <> 0 And TxtPorcentagemMuscEsqueceletico <> "" Then
683:  TxtPesoMuscular = Format((CDbl(FrmAvaliacaoB.TxtPorcentagemMuscEsqueceletico) / 100) * CDbl(FrmAvaliacaoB.TxtPesoKg), "#,##0.00")
684:  CheckPesoMuscular.Visible = True
687:  TxtIntervaloMusculo = TabelaMusculoEsqOMR_INTERVALO(TxtPorcentagemMuscEsqueceletico, TxtSexo, TxtIdade)
689:  End If
691: End Sub
```

## FrmAnamnese.frm — CommandButton184_Click

Origem: `FrmAnamnese.frm:108`.

```text
108: Private Sub CommandButton184_Click()
109: On Error Resume Next
110:     Dim sistolica As Double
111:     Dim diastolica As Double
112:     Dim classificacao As String
115:     sistolica = CDbl(TxtPAS)
116:     diastolica = CDbl(TxtPAD)
118: If sistolica And diastolica <> 0 Then
121:     If sistolica < 90 And diastolica < 60 Then
122:         classificacao = "Baixa"
124:       ElseIf sistolica < 120 And diastolica < 80 Then
125:         classificacao = "Ótima"
127:     ElseIf sistolica < 130 And diastolica < 85 Then
128:         classificacao = "Normal"
130:             ElseIf sistolica < 140 And diastolica < 90 Then
131:         classificacao = "Limítrofe"
133:     ElseIf sistolica < 160 And diastolica < 100 Then
134:         classificacao = "Hipertensão Estágio 1"
136:     ElseIf sistolica < 180 And diastolica < 110 Then
137:         classificacao = "Hipertensão Estágio 2"
139:     ElseIf sistolica >= 180 And diastolica >= 110 Then
140:         classificacao = "Hipertensão Estágio 3"
142:     ElseIf sistolica >= 140 And diastolica < 90 Then
143:         classificacao = "Sistólica Isolada"
146:     Else
147:         classificacao = "Não Classificado"
148:     End If
150:    TxtPACls = classificacao
152:  Else
153:  MsgBox "Insira Pressão Sistolica e Diastolica para classificar"
155:  End If
156: End Sub
```

## FrmAnamnese.frm — CommandButton185_Click

Origem: `FrmAnamnese.frm:158`.

```text
158: Private Sub CommandButton185_Click()
159: On Error Resume Next
161: If TxtFcREP <> 0 Then
163:     Dim Idade As Integer
164:     Dim frequenciaCardiacaRepouso As Double
165:     Dim frequenciaCardiacaMaxima As Double
166:     Dim frequenciaCardiacaReserva As Double
169:     Idade = TxtIdade
170:     frequenciaCardiacaRepouso = TxtFcREP
172: FrmFcMax.TxtSexo = TxtSexo
173:   FrmFcMax.TxtIdade = TxtIdade
175:   FrmFcMax.Show
178:   frequenciaCardiacaMaxima = TxtFcMAX
181:     frequenciaCardiacaReserva = frequenciaCardiacaMaxima - frequenciaCardiacaRepouso
183:     TxtFcMAX = Format(frequenciaCardiacaMaxima, "#,##0.00")
184:     TxtFcRES = Format(frequenciaCardiacaReserva, "#,##0.00")
186:  Else
187:  MsgBox "Insira a Frequência Cardíaca em Repouso antes de prosseguir!"
188:  Exit Sub
189:  End If
191: End Sub
```

## FrmFcMax.frm — CommandButton179_Click

Origem: `FrmFcMax.frm:11`.

```text
11: Private Sub CommandButton179_Click()
12: On Error Resume Next
13: Dim Sexo As String
14: Dim Idade As Double
15: Dim FcMax As Double
17: Sexo = TxtSexo
18: Idade = TxtIdade
21: If Sexo = "Feminino" Or Sexo = "Masculino" Then
23: If Obtn1.Value = True Then
25: FcMax = 220 - Idade
26: TxtFCMAXX = Format(Application.WorksheetFunction.RoundUp(FcMax, 0), "#,##0.00")
27: Exit Sub
29: ElseIf Obtn2.Value = True Then
31: FcMax = 220 - (0.65 * Idade)
32: TxtFCMAXX = Format(Application.WorksheetFunction.RoundUp(FcMax, 0), "#,##0.00")
33: Exit Sub
35: ElseIf Obtn3.Value = True Then
37: FcMax = 208 - (0.7 * Idade)
38: TxtFCMAXX = Format(Application.WorksheetFunction.RoundUp(FcMax, 0), "#,##0.00")
39: Exit Sub
41: End If
43: End If
45: If Sexo = "Masculino" Then
47: If Obtn4.Value = True Then
49: FcMax = 201 - (0.6 * Idade)
50: TxtFCMAXX = Format(Application.WorksheetFunction.RoundUp(FcMax, 0), "#,##0.00")
51: Exit Sub
54: ElseIf Obtn5.Value = True Then
56: FcMax = 205 - (0.41 * Idade)
57: TxtFCMAXX = Format(Application.WorksheetFunction.RoundUp(FcMax, 0), "#,##0.00")
58: Exit Sub
61: ElseIf Obtn6.Value = True Then
63: FcMax = 198 - (0.41 * Idade)
64: TxtFCMAXX = Format(Application.WorksheetFunction.RoundUp(FcMax, 0), "#,##0.00")
65: Exit Sub
68: ElseIf Obtn7.Value = True Then
70: FcMax = 205 - (0.7 * Idade)
71: TxtFCMAXX = Format(Application.WorksheetFunction.RoundUp(FcMax, 0), "#,##0.00")
72: Exit Sub
74: End If
75: End If
77: If Sexo = "Feminino" Then
78: If Obtn8.Value = True Then
80: FcMax = 192 - (0.7 * Idade)
81: TxtFCMAXX = Format(Application.WorksheetFunction.RoundUp(FcMax, 0), "#,##0.00")
82: Exit Sub
84: ElseIf Obtn9.Value = True Then
86: FcMax = 206 - (0.597 * Idade)
87: TxtFCMAXX = Format(Application.WorksheetFunction.RoundUp(FcMax, 0), "#,##0.00")
88: Exit Sub
90: End If
91: End If
92: End Sub
```

## FrmAgua.frm — CommandButton1_Click

Origem: `FrmAgua.frm:14`.

```text
14: Private Sub CommandButton1_Click()
17: If TextBox1 = "" Then
18: MsgBox "Informe o peso em kg."
19: End If
21: If TextBox2 = "" Then
22: MsgBox "Informe ML por quilo."
23: End If
25: TextBox3 = Format(CDbl(TextBox1) * CDbl(TextBox2), "#,##0.00")
26: End Sub
```

## FrmUrina.frm — CommandButton1_Click

Origem: `FrmUrina.frm:10`.

```text
10: Private Sub CommandButton1_Click()
11: On Error Resume Next
13: If OptionButton1.Value = True Then
14: TextBox1 = "Bem Hidratado"
15: TextBox2 = Format(1, "#,##0.00")
16: ElseIf OptionButton2.Value = True Then
17: TextBox1 = "Bem Hidratado"
18: TextBox2 = Format(2, "#,##0.00")
19: ElseIf OptionButton3.Value Or OptionButton4.Value = True Then
20: TextBox1 = "Levemente Desidratado"
21: TextBox2 = Format(3, "#,##0.00")
22: ElseIf OptionButton4.Value = True Then
23: TextBox1 = "Levemente Desidratado"
24: TextBox2 = Format(4, "#,##0.00")
25: ElseIf OptionButton5.Value = True Then
26: TextBox1 = "Moderadamente Desidratado"
27: TextBox2 = Format(5, "#,##0.00")
28: ElseIf OptionButton6.Value = True Then
29: TextBox1 = "Moderadamente Desidratado"
30: TextBox2 = Format(6, "#,##0.00")
31: ElseIf OptionButton7.Value = True Then
32: TextBox1 = "Severamente Desidratado"
33: TextBox2 = Format(7, "#,##0.00")
34: ElseIf OptionButton8.Value = True Then
35: TextBox1 = "Severamente Desidratado"
36: TextBox2 = Format(8, "#,##0.00")
37: Else
38: TextBox1 = "Não Calculado"
39: TextBox2 = Format(0, "#,##0.00")
40: Exit Sub
41: End If
44: End Sub
```

## FrmNecessidadeEnergetica.frm — CommandButton145_Click

Origem: `FrmNecessidadeEnergetica.frm:15`.

```text
15: Private Sub CommandButton145_Click()
16: On Error Resume Next
17: If TxtTmb = "" Or TxtTmb = 0 Then
18: MsgBox "Calcule o TBM para prosseguir!"
19: TxtTmb.SetFocus
20: Exit Sub
21: End If
23: If CbFa = "" Then
24: MsgBox "Insira um fator para calcular!"
25: CbFa.SetFocus
26: Exit Sub
27: End If
29: TxtGet = Format(CDbl(CbFa) * CDbl(TxtTmb), "#,##0.00")
31: End Sub
```

## FrmNecessidadeEnergetica.frm — CommandButton2_Click

Origem: `FrmNecessidadeEnergetica.frm:33`.

```text
33: Private Sub CommandButton2_Click()
34: On Error Resume Next
35: If TxtPesoAtual = "" Or TxtPesoAtual = 0 Then
36: MsgBox "Insira o peso para prosseguir!"
37: TxtPesoAtual.BackColor = vbRed
38: TxtPesoAtual.SetFocus
39: Exit Sub
40: End If
42: If TxtAlturaAtual = "" Or TxtAlturaAtual = 0 Then
43: MsgBox "Insira a altura para prosseguir!"
44: TxtAlturaAtual.BackColor = vbRed
45: TxtAlturaAtual.SetFocus
46: Exit Sub
47: End If
49: If ObtnHB.Value = False And ObtnOMS.Value = False And ObtnDRI_A.Value = False And ObtnDRI_CA.Value = False And ObtnBioimpedancia.Value = False Then
50: MsgBox "Selecione uma opção para calcular!"
51: ObtnHB.BackColor = vbRed
52: ObtnHB.SetFocus
53: Exit Sub
54: End If
56: If ObtnBioimpedancia.Value = True Then
57: If TxtProtocolo = "Bioimpedância R" Then
58: TxtTmb = Format(FrmAvaliacaoB.TxtTmb, "#,##0.00")
59: ElseIf TxtProtocolo = "Bioimpedância C" Then
60: TxtTmb = Format(FrmAvaliacaoC.TxtTmb, "#,##0.00")
61: ElseIf TxtProtocolo = "Bioimpedância A" Then
62: TxtTmb = Format(FrmAvaliacaoA.TxtGeb, "#,##0.00")
63: End If
64: Exit Sub
65: End If
67: If ObtnHB.Value = True Then
68: TxtTmb = Format(Harris_Benedict(TxtSexo, TxtPesoAtual, TxtAlturaAtual, TxtIdade), "#,##0.00")
69: Exit Sub
70: End If
72: If ObtnOMS.Value = True Then
73: TxtTmb = Format(tmbOMS(TxtSexo, TxtPesoAtual, TxtIdade), "#,##0.00")
74: Exit Sub
75: End If
77: If ObtnDRI_CA.Value = True Then
78: TxtTmb = Format(DRIS_Adolescentes(TxtSexo, TxtPesoAtual, TxtAlturaAtual, TxtIdade), "#,##0.00")
79: Exit Sub
81: ElseIf ObtnDRI_A.Value = True Then
82: TxtTmb = Format(DRIS_Adultos(TxtSexo, TxtPesoAtual, TxtAlturaAtual, TxtIdade), "#,##0.00")
83: Exit Sub
84: End If
86: End Sub
```

## FrmNecessidadeEnergetica.frm — UserForm_Initialize

Origem: `FrmNecessidadeEnergetica.frm:183`.

```text
183: Private Sub UserForm_Initialize()
184: On Error Resume Next
186: CbFa.Clear
187: CbFa.AddItem 1
188: CbFa.AddItem 1.2
189: CbFa.AddItem 1.3
190: CbFa.AddItem 1.4
191: CbFa.AddItem 1.5
192: CbFa.AddItem 1.55
193: CbFa.AddItem 1.56
194: CbFa.AddItem 1.6
196: CbFa.AddItem 1.64
197: CbFa.AddItem 1.78
198: CbFa.AddItem 1.8
199: CbFa.AddItem 1.82
200: CbFa.AddItem 1.9
201: CbFa.AddItem 2.1
202: CbFa.AddItem 2.2
203: CbFa.AddItem 2.5
204: CbFa.AddItem 3
205: CbFa.AddItem 3.5
206: CbFa.AddItem 4
207: CbFa.AddItem 5
208: CbFa.AddItem 6
209: CbFa = 1
210: End Sub
```

