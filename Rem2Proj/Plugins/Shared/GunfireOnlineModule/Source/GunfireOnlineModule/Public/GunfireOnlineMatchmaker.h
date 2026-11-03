#pragma once
#include "CoreMinimal.h"
#include "EMatchmakingRegions.h"
#include "ESessionSearchType.h"
#include "GunfireOnlineSubsystemBase.h"
#include "OnMatchmakeDelegateDelegate.h"
#include "OnMatchmakeFailDelegateDelegate.h"
#include "OnMatchmakeSuccessDelegateDelegate.h"
#include "GunfireOnlineMatchmaker.generated.h"

class UGunfireOnlineMatchmaker;
class UObject;

UCLASS(Blueprintable)
class GUNFIREONLINEMODULE_API UGunfireOnlineMatchmaker : public UGunfireOnlineSubsystemBase {
    GENERATED_BODY()
public:
    UPROPERTY(BlueprintAssignable, BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    FOnMatchmakeDelegate OnMatchmakeBegin;
    
    UPROPERTY(BlueprintAssignable, BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    FOnMatchmakeFailDelegate OnMatchmakeFail;
    
    UPROPERTY(BlueprintAssignable, BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    FOnMatchmakeSuccessDelegate OnMatchmakeSuccess;
    
    UGunfireOnlineMatchmaker();

    UFUNCTION(BlueprintCallable)
    void SetMatchmakingRegion(EMatchmakingRegions Region);
    
    UFUNCTION(BlueprintCallable)
    void Matchmake(ESessionSearchType NewSearchType, int32 MaxSessions, float Timeout);
    
    UFUNCTION(BlueprintCallable, BlueprintPure)
    bool IsFindingSessions() const;
    
    UFUNCTION(BlueprintCallable, BlueprintPure)
    EMatchmakingRegions GetMatchmakingRegion() const;
    
    UFUNCTION(BlueprintCallable, BlueprintPure, meta=(WorldContext="WorldContextObject"))
    static UGunfireOnlineMatchmaker* GetGunfireOnlineMatchmaker(const UObject* WorldContextObject);
    
    UFUNCTION(BlueprintCallable, BlueprintPure)
    EMatchmakingRegions DetectMatchmakingRegion() const;
    
    UFUNCTION(BlueprintCallable)
    void CancelFindSessions();
    
};

