# 치과 키우기

원내생에서 시작해 치과병원까지 키우는 클리커 게임. 기획은 옵시디안 `Project/dc-clicker` 노트에 있다.

```
src/Dc.Core     게임 규칙 · 저장 (플랫폼 없음)
src/Dc.Ui       화면 · 입력 (SkiaSharp, PC · 안드로이드 공용)
src/Dc.Desktop  PC 창
tests/Dc.Tests  테스트
assets/images   그림 — docs/image-prompts.md 의 파일 이름으로 넣는다
```

## 실행

```
dotnet run --project src/Dc.Desktop
dotnet test
```

클릭이나 Space 로 직접 진료한다. 저장 파일은 `%LOCALAPPDATA%\DentalClinicClicker\save.json`.
