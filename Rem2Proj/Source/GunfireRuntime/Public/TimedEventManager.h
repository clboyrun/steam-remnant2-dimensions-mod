#pragma once
#include "CoreMinimal.h"
#include "Subsystems/GameInstanceSubsystem.h"
#include "TimedEvent.h"
#include "TimedEventManager.generated.h"

class URootVariableContext;
class UTitleStorageFile;
class UVariableComponent;

UCLASS(Blueprintable, DefaultConfig, Config=Engine)
class GUNFIRERUNTIME_API UTimedEventManager : public UGameInstanceSubsystem {
    GENERATED_BODY()
public:
private:
    UPROPERTY(BlueprintReadWrite, EditAnywhere, Instanced, meta=(AllowPrivateAccess=true))
    UVariableComponent* VariableComponent;
    
public:
    UTimedEventManager();

protected:
    UFUNCTION(BlueprintCallable)
    void OnRootVariableContextCreated(URootVariableContext* Context);
    
    UFUNCTION(BlueprintCallable)
    void OnFileManifestDownloaded(bool bSuccess, const TArray<UTitleStorageFile*>& FileManifest);
    
    UFUNCTION(BlueprintCallable)
    void OnFileDownloaded(bool bSuccess, UTitleStorageFile* File);
    
public:
    UFUNCTION(BlueprintCallable, BlueprintPure)
    bool HasTags(const TArray<FName>& RequiredTags) const;
    
    UFUNCTION(BlueprintCallable, BlueprintPure)
    bool HasEvent(FName EventName) const;
    
    UFUNCTION(BlueprintCallable, BlueprintPure)
    TArray<FName> GetTags() const;
    
    UFUNCTION(BlueprintCallable, BlueprintPure)
    TArray<FTimedEvent> GetEvents() const;
    
    UFUNCTION(BlueprintCallable, BlueprintPure)
    bool GetEvent(FName EventName, UPARAM(Ref) FTimedEvent& OutEvent) const;
    
};

