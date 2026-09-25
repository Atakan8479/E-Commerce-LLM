SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DELETE FROM dbo.AlternativeRecommendationPlans
WHERE OriginalProductId IN
(
    '11111111-1111-1111-1111-111111111111',
    '22222222-2222-2222-2222-222222222222',
    '44444444-4444-4444-4444-444444444444'
);

MERGE dbo.Products AS target
USING
(
    VALUES
    (
        CAST(
            '11111111-1111-1111-1111-111111111111'
            AS uniqueidentifier
        ),
        N'Wireless Gaming Mouse',
        N'gaming-mouse'
    ),
    (
        CAST(
            '22222222-2222-2222-2222-222222222222'
            AS uniqueidentifier
        ),
        N'Wireless Gaming Mouse Pro',
        N'gaming-mouse'
    ),
    (
        CAST(
            '44444444-4444-4444-4444-444444444444'
            AS uniqueidentifier
        ),
        N'RGB Gaming Mouse',
        N'gaming-mouse'
    )
) AS source
(
    Id,
    Name,
    Category
)
ON target.Id = source.Id

WHEN MATCHED THEN
    UPDATE SET
        Name =
            source.Name,
        Category =
            source.Category

WHEN NOT MATCHED THEN
    INSERT
    (
        Id,
        Name,
        Category
    )
    VALUES
    (
        source.Id,
        source.Name,
        source.Category
    );

MERGE dbo.InventoryItems AS target
USING
(
    VALUES
    (
        CAST(
            '11111111-1111-1111-1111-111111111111'
            AS uniqueidentifier
        ),
        5
    ),
    (
        CAST(
            '22222222-2222-2222-2222-222222222222'
            AS uniqueidentifier
        ),
        1
    ),
    (
        CAST(
            '44444444-4444-4444-4444-444444444444'
            AS uniqueidentifier
        ),
        0
    )
) AS source
(
    ProductId,
    AvailableQuantity
)
ON target.ProductId =
    source.ProductId

WHEN MATCHED THEN
    UPDATE SET
        AvailableQuantity =
            source.AvailableQuantity

WHEN NOT MATCHED THEN
    INSERT
    (
        ProductId,
        AvailableQuantity
    )
    VALUES
    (
        source.ProductId,
        source.AvailableQuantity
    );

COMMIT TRANSACTION;

SELECT
    p.Id,
    p.Name,
    p.Category,
    i.AvailableQuantity
FROM dbo.Products AS p
INNER JOIN dbo.InventoryItems AS i
    ON i.ProductId = p.Id
WHERE p.Id IN
(
    '11111111-1111-1111-1111-111111111111',
    '22222222-2222-2222-2222-222222222222',
    '44444444-4444-4444-4444-444444444444'
)
ORDER BY p.Id;

GO