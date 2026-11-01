#pragma once

#include "CoreMinimal.h"
#include "Factories/Factory.h"
#include "DimensionsObjectFactory.generated.h"

/**
 * Creates a standalone asset of any non-abstract UObject class. The Dimensions editor script sets ObjectClass and
 * calls AssetTools.create_asset with it. Never shown in the editor's "new asset" menus.
 */
UCLASS()
class DIMENSIONSEDITOR_API UDimensionsObjectFactory : public UFactory
{
    GENERATED_BODY()

public:
    UDimensionsObjectFactory();

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Dimensions")
    TSubclassOf<UObject> ObjectClass;

    virtual UObject* FactoryCreateNew(UClass* InClass, UObject* InParent, FName InName, EObjectFlags Flags,
        UObject* Context, FFeedbackContext* Warn) override;
    virtual bool DoesSupportClass(UClass* Class) override { return true; }
    virtual UClass* ResolveSupportedClass() override;
    virtual bool ShouldShowInNewMenu() const override { return false; }
};
