#pragma once
#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "ClientDamageInfo.h"
#include "ClientEvadeRecord.h"
#include "PendingDamageInfo.h"
#include "DamageValidationComponent.generated.h"

class UObject;

UCLASS(Blueprintable, ClassGroup=Custom, meta=(BlueprintSpawnableComponent))
class GUNFIRERUNTIME_API UDamageValidationComponent : public UActorComponent {
    GENERATED_BODY()
public:
protected:
    UPROPERTY(BlueprintReadWrite, EditAnywhere, Transient, meta=(AllowPrivateAccess=true))
    TArray<FPendingDamageInfo> ValidatingDamage;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, Transient, meta=(AllowPrivateAccess=true))
    TArray<FClientEvadeRecord> EvadeRecords;
    
public:
    UDamageValidationComponent(const FObjectInitializer& ObjectInitializer);

    UFUNCTION(BlueprintCallable, Reliable, Server)
    void ServerReceiveDamageResults(uint8 DamageID, bool bWasEvaded, bool bWasCounterEvade, const UObject* Source);
    
    UFUNCTION(Client, Reliable)
    void ClientValidateDamage(uint8 DamageID, uint16 DamageTimestamp, const FClientDamageInfo& DamageInfo);
    
};

