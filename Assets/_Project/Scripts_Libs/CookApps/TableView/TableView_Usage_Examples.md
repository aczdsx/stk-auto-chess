# TableView 사용 가이드

TableView를 Fluent Builder 패턴으로 간단하게 사용할 수 있습니다.

## 기본 사용법

```csharp
using RabbitDog.UIExtensions;
using UnityEngine;
using System.Collections.Generic;

public class SimpleShopView : MonoBehaviour
{
    [SerializeField] private TableView tableView;
    [SerializeField] private GameObject productCellPrefab;

    private TableViewController<ProductData, ProductCell> controller;
    private List<ProductData> products = new();

    private void Start()
    {
        // 데이터 준비
        products.Add(new ProductData { name = "상품 1", price = 1000 });
        products.Add(new ProductData { name = "상품 2", price = 2000 });
        products.Add(new ProductData { name = "상품 3", price = 3000 });

        // TableView 설정 (Fluent Builder)
        controller = tableView.CreateController<ProductData, ProductCell>()
            .WithData(products)
            .WithCellPrefab(productCellPrefab)
            .WithCellSize(new Vector2(400, 100))  // 고정 크기
            .OnBind((cell, data, index) =>
            {
                // 데이터 바인딩
                cell.nameText.text = data.name;
                cell.priceText.text = $"{data.price} 골드";
            })
            .Build();
    }

    private void OnDestroy()
    {
        // Controller 정리
        controller?.Detach();
    }
}
```

## 동적 크기 사용

```csharp
controller = tableView.CreateController<ProductData, ProductCell>()
    .WithData(products)
    .WithCellPrefab(productCellPrefab)
    .WithCellSize(data =>
    {
        // 데이터에 따라 크기 동적 결정
        float height = data.hasDiscount ? 120 : 100;
        return new Vector2(400, height);
    })
    .OnBind((cell, data, index) =>
    {
        cell.UpdateUI(data);
    })
    .Build();
```

## 셀 팩토리 커스터마이징

```csharp
controller = tableView.CreateController<ProductData, ProductCell>()
    .WithData(products)
    .WithCellFactory(() =>
    {
        // 커스텀 생성 로직
        var go = Instantiate(productCellPrefab);
        go.name = $"ProductCell_{System.Guid.NewGuid()}";
        return go;
    })
    .WithCellSize(new Vector2(400, 100))
    .OnBind((cell, data, index) =>
    {
        cell.UpdateUI(data);
    })
    .OnCellCreated(cell =>
    {
        // 셀이 최초 생성될 때 한 번만 실행
        Debug.Log($"Cell created: {cell.name}");
    })
    .OnCellRecycled(cell =>
    {
        // 셀이 재사용될 때마다 실행 (바인딩 전)
        cell.ResetState();
    })
    .Build();
```

## 런타임 데이터 업데이트

```csharp
// 데이터 추가
controller.AddData(newProduct);

// 데이터 제거
controller.RemoveData(productToRemove);

// 전체 데이터 교체
controller.SetData(newProductList);

// 데이터 클리어
controller.ClearData();

// 수동 새로고침
controller.Refresh();

// 현재 데이터 가져오기
List<ProductData> currentData = controller.GetDataList();
```

## 고급 사용 예제

### 클릭 이벤트 처리

```csharp
controller = tableView.CreateController<ProductData, ProductCell>()
    .WithData(products)
    .WithCellPrefab(productCellPrefab)
    .WithCellSize(new Vector2(400, 100))
    .OnBind((cell, data, index) =>
    {
        cell.nameText.text = data.name;
        cell.priceText.text = $"{data.price} 골드";

        // 클릭 이벤트 설정
        cell.buyButton.onClick.RemoveAllListeners();
        cell.buyButton.onClick.AddListener(() => OnProductClicked(data, index));
    })
    .OnCellReleased(cell =>
    {
        // 셀이 반환될 때 리스너 정리
        cell.buyButton.onClick.RemoveAllListeners();
    })
    .Build();

private void OnProductClicked(ProductData data, int index)
{
    Debug.Log($"Product clicked: {data.name} at index {index}");
    // 구매 로직...
}
```

### 인덱스 기반 동적 크기

```csharp
controller = tableView.CreateController<ProductData, ProductCell>()
    .WithData(products)
    .WithCellPrefab(productCellPrefab)
    .WithCellSize((data, index) =>
    {
        // 첫 번째 아이템은 더 크게
        if (index == 0)
            return new Vector2(400, 150);

        // 특별 상품은 크게
        if (data.isSpecialOffer)
            return new Vector2(400, 120);

        return new Vector2(400, 100);
    })
    .OnBind((cell, data, index) =>
    {
        cell.UpdateUI(data);
    })
    .Build();
```

### 셀 상태 관리

```csharp
controller = tableView.CreateController<ProductData, ProductCell>()
    .WithData(products)
    .WithCellPrefab(productCellPrefab)
    .WithCellSize(new Vector2(400, 100))
    .OnCellCreated(cell =>
    {
        // 최초 생성 시 한 번만 실행
        cell.Initialize();
        Debug.Log($"Cell {cell.gameObject.GetInstanceID()} created");
    })
    .OnCellRecycled(cell =>
    {
        // 재사용 시 상태 초기화 (바인딩 전)
        cell.ResetState();
        cell.gameObject.SetActive(true);
    })
    .OnBind((cell, data, index) =>
    {
        // 데이터 바인딩
        cell.nameText.text = data.name;
        cell.priceText.text = $"{data.price} 골드";
    })
    .OnCellReleased(cell =>
    {
        // 풀로 반환 시 정리
        cell.ClearEventListeners();
    })
    .Build();
```

## 기존 코드 마이그레이션

### Before (기존 방식)

```csharp
private ObjectPool<GameObject> tableViewPool;
private List<ProductData> dataList = new();

private void Awake()
{
    // 수동 Pool 생성
    tableViewPool = new ObjectPool<GameObject>(
        () => {
            var go = Instantiate(cellPrefab);
            go.transform.SetParent(tableView.content, false);
            return go;
        },
        obj => obj.SetActive(true),
        obj => obj.SetActive(false),
        Destroy,
        false
    );

    // 수동 이벤트 연결
    tableView.OnGetTotalCellItemCount += OnGetTotalTableViewCellItemCount;
    tableView.OnGetCellItemSize += OnGetTableViewCellItemSize;
    tableView.OnReleaseCellItem += OnReleaseTableViewCellItem;
    tableView.OnGetCellItem += OnGetTableViewCellItem;
}

private int OnGetTotalTableViewCellItemCount()
{
    return dataList.Count;
}

private Vector2 OnGetTableViewCellItemSize(int idx)
{
    return new Vector2(400, 100);
}

private GameObject OnGetTableViewCellItem(int idx)
{
    var go = tableViewPool.Get();
    var cell = go.GetComponent<ProductCell>();
    cell.UpdateUI(dataList[idx]);
    return go;
}

private void OnReleaseTableViewCellItem(GameObject obj)
{
    tableViewPool.Release(obj);
}

private void OnDestroy()
{
    tableViewPool.Dispose();
}
```

### After (새로운 방식)

```csharp
private TableViewController<ProductData, ProductCell> controller;
private List<ProductData> dataList = new();

private void Start()
{
    controller = tableView.CreateController<ProductData, ProductCell>()
        .WithData(dataList)
        .WithCellPrefab(cellPrefab)
        .WithCellSize(new Vector2(400, 100))
        .OnBind((cell, data, index) => cell.UpdateUI(data))
        .Build();
}

private void OnDestroy()
{
    controller?.Detach();
}
```

**코드 라인 수: 40줄 → 10줄 (75% 감소)**

## 빌더 메서드 참고

### 필수 메서드

- `WithCellPrefab(GameObject)` 또는 `WithCellFactory(Func<GameObject>)` - 셀 생성 방법 지정
- `Build()` - 설정 완료 및 TableView 연결

### 선택 메서드

- `WithData(List<TData>)` - 초기 데이터 설정
- `WithCellSize(Vector2)` - 고정 셀 크기
- `WithCellSize(Func<TData, Vector2>)` - 데이터 기반 동적 크기
- `WithCellSize(Func<TData, int, Vector2>)` - 데이터 + 인덱스 기반 크기
- `OnBind(Action<TCell, TData, int>)` - 셀 바인딩 콜백
- `OnCellCreated(Action<TCell>)` - 셀 최초 생성 콜백
- `OnCellRecycled(Action<TCell>)` - 셀 재사용 콜백
- `OnCellReleased(Action<TCell>)` - 셀 반환 콜백

## 팁 & 주의사항

### 1. Detach 꼭 호출하기

```csharp
private void OnDestroy()
{
    controller?.Detach();  // 필수!
}
```

`Detach()`를 호출하지 않으면 메모리 누수와 이벤트 중복 연결 문제가 발생할 수 있습니다.

### 2. 데이터 변경 시 Refresh

```csharp
// 자동 Refresh
controller.SetData(newList);  // OK - 자동으로 새로고침
controller.AddData(item);     // OK - 자동으로 새로고침

// 수동 Refresh 필요
dataList.Add(item);
controller.Refresh();  // 필수! 직접 dataList를 수정했으면 Refresh 호출
```

### 3. Build() 호출 필수

```csharp
// 잘못된 예
var controller = tableView.CreateController<ProductData, ProductCell>()
    .WithCellPrefab(prefab)
    .WithCellSize(new Vector2(100, 100));
// Build()를 호출하지 않으면 아무것도 작동하지 않음!

// 올바른 예
var controller = tableView.CreateController<ProductData, ProductCell>()
    .WithCellPrefab(prefab)
    .WithCellSize(new Vector2(100, 100))
    .Build();  // 반드시 호출!
```

### 4. 셀 컴포넌트 필수

```csharp
// TCell은 반드시 Component를 상속해야 함
public class ProductCell : MonoBehaviour  // OK
{
    public TMPro.TextMeshProUGUI nameText;
    public TMPro.TextMeshProUGUI priceText;

    public void UpdateUI(ProductData data)
    {
        nameText.text = data.name;
        priceText.text = $"{data.price}";
    }
}
```

### 5. 이벤트 리스너 정리

셀에 버튼이나 이벤트 리스너가 있다면 반드시 정리해야 합니다:

```csharp
.OnBind((cell, data, index) =>
{
    cell.button.onClick.RemoveAllListeners();  // 먼저 제거
    cell.button.onClick.AddListener(() => OnClick(data));  // 그 다음 추가
})
.OnCellReleased(cell =>
{
    cell.button.onClick.RemoveAllListeners();  // 반환 시 정리
})
```

## 자동으로 처리되는 것들

TableViewController가 자동으로 관리해주는 것들:

- ✅ ObjectPool 생성/관리/정리
- ✅ TableView 이벤트 연결/해제
- ✅ 셀 활성화/비활성화
- ✅ 데이터 카운트 관리
- ✅ 부모 Transform 설정

## 성능 팁

1. **OnBind는 가볍게** - 셀이 스크롤될 때마다 호출되므로 무거운 작업은 OnCellCreated에서 처리
2. **리스너는 반드시 정리** - RemoveAllListeners()로 이전 리스너 제거
3. **데이터는 참조로** - 데이터를 복사하지 말고 참조로 전달
4. **GetDataList()는 원본 반환** - 반환된 리스트를 수정하면 원본도 바뀌므로 주의

```csharp
// 좋은 예
List<ProductData> copy = new List<ProductData>(controller.GetDataList());
copy.Add(newItem);  // 복사본 수정
controller.SetData(copy);  // 다시 설정

// 나쁜 예
controller.GetDataList().Add(newItem);  // 원본 직접 수정
// Refresh가 자동으로 안 됨!
```
