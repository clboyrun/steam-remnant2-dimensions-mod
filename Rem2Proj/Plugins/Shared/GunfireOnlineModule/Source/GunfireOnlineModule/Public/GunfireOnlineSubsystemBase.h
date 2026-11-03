#pragma once
#include "CoreMinimal.h"
#include "Subsystems/GameInstanceSubsystem.h"
#include "EGunfireOnlineEvent.h"
#include "OnOnlineFailEventDelegate.h"
#include "OnOnlineSessionWaitDialogDelegate.h"
#include "GunfireOnlineSubsystemBase.generated.h"

UCLASS(Abstract, Blueprintable)
class GUNFIREONLINEMODULE_API UGunfireOnlineSubsystemBase : public UGameInstanceSubsystem {
    GENERATED_BODY()
public:
    UPROPERTY(BlueprintAssignable, BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    FOnOnlineSessionWaitDialog OnWaitDialog;
    
    UPROPERTY(BlueprintAssignable, BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    FOnOnlineFailEvent OnMinorOnlineFailEvent;
    
    UGunfireOnlineSubsystemBase();

    UFUNCTION(BlueprintCallable)
    void GetOnlineEventMessage(EGunfireOnlineEvent Event, FText& OutHeader, FText& OutBody);
    
    UFUNCTION(BlueprintCallable)
    bool CanPlayOnline();
    
};

