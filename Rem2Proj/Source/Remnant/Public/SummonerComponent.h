#pragma once
#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "Engine/EngineTypes.h"
#include "SummonerActionEventDelegate.h"
#include "SummonerEventDelegate.h"
#include "Templates/SubclassOf.h"
#include "SummonerComponent.generated.h"

class AActor;

UCLASS(Blueprintable, ClassGroup=Custom, meta=(BlueprintSpawnableComponent))
class REMNANT_API USummonerComponent : public UActorComponent {
    GENERATED_BODY()
public:
    UPROPERTY(BlueprintAssignable, BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    FSummonerActionEvent OnSummonActionEvent;
    
    UPROPERTY(BlueprintAssignable, BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    FSummonerEvent OnSummonAdded;
    
    UPROPERTY(BlueprintAssignable, BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    FSummonerEvent OnSummonRemoved;
    
    USummonerComponent(const FObjectInitializer& ObjectInitializer);

    UFUNCTION(BlueprintCallable)
    void RemoveSummon(AActor* Summon);
    
    UFUNCTION(BlueprintCallable)
    void RegisterSummon(AActor* Summon);
    
    UFUNCTION(BlueprintCallable)
    void OnSummonEndPlay(AActor* Summon, const TEnumAsByte<EEndPlayReason::Type> EndPlayReason);
    
    UFUNCTION(BlueprintCallable, BlueprintPure)
    bool HasSummon(TSubclassOf<AActor> SummonClass);
    
    UFUNCTION(BlueprintCallable, BlueprintPure)
    TArray<AActor*> GetSummonsOfType(TSubclassOf<AActor> SummonClass);
    
    UFUNCTION(BlueprintCallable, BlueprintPure)
    TArray<AActor*> GetSummons();
    
    UFUNCTION(BlueprintCallable, BlueprintPure)
    int32 GetNumSummonsOfType(TSubclassOf<AActor> SummonClass);
    
    UFUNCTION(BlueprintCallable)
    void BroadcastSummonActionEvent(AActor* Summon, FName EventName, AActor* RelevantEventTarget);
    
};

