#pragma once
#include "CoreMinimal.h"
#include "SpawnTableEntry.h"
#include "Templates/SubclassOf.h"
#include "SpawnTableCharacterMod.generated.h"

class UActionBase;

UCLASS(Blueprintable)
class REMNANT_API USpawnTableCharacterMod : public USpawnTableEntry {
    GENERATED_BODY()
public:
    UPROPERTY(BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    TSubclassOf<UActionBase> Modifier;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    TSoftClassPtr<UActionBase> ModifierClass;
    
    USpawnTableCharacterMod();

};

