#pragma once
#include "CoreMinimal.h"
#include "GunfireOnlineSubsystemBase.h"
#include "OnTitleStorageFileDownloadProgressDelegate.h"
#include "OnTitleStorageFileDownloadedDelegate.h"
#include "OnTitleStorageFileManifestDownloadedDelegate.h"
#include "GunfireOnlineTitleStorage.generated.h"

class UGunfireOnlineTitleStorage;
class UObject;
class UTitleStorageFile;

UCLASS(Blueprintable)
class GUNFIREONLINEMODULE_API UGunfireOnlineTitleStorage : public UGunfireOnlineSubsystemBase {
    GENERATED_BODY()
public:
    UPROPERTY(BlueprintAssignable, BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    FOnTitleStorageFileManifestDownloaded OnFileManifestDownloaded;
    
    UPROPERTY(BlueprintAssignable, BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    FOnTitleStorageFileDownloaded OnFileDownloaded;
    
    UPROPERTY(BlueprintAssignable, BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    FOnTitleStorageFileDownloadProgress OnFileDownloadProgress;
    
private:
    UPROPERTY(BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    TArray<UTitleStorageFile*> CachedFiles;
    
public:
    UGunfireOnlineTitleStorage();

protected:
    UFUNCTION()
    void OnReadFileProgress(const FString& Filename, uint64 NumBytes);
    
    UFUNCTION(BlueprintCallable)
    void OnReadFileComplete(bool bWasSuccessful, const FString& Filename);
    
    UFUNCTION(BlueprintCallable)
    void OnEnumerateFilesComplete(bool bWasSuccessful, const FString& Error);
    
public:
    UFUNCTION(BlueprintCallable, BlueprintPure)
    bool IsBusy() const;
    
    UFUNCTION(BlueprintCallable, BlueprintPure, meta=(WorldContext="WorldContextObject"))
    static UGunfireOnlineTitleStorage* GetInstance(const UObject* WorldContextObject);
    
    UFUNCTION(BlueprintCallable)
    TArray<UTitleStorageFile*> GetFileManifest();
    
    UFUNCTION(BlueprintCallable)
    UTitleStorageFile* GetFile(const FString& Filename);
    
    UFUNCTION(BlueprintCallable)
    bool DownloadFileManifest();
    
    UFUNCTION(BlueprintCallable)
    bool DownloadFile(const FString& Filename);
    
};

