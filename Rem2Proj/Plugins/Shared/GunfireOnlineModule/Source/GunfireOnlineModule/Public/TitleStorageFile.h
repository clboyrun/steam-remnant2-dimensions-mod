#pragma once
#include "CoreMinimal.h"
#include "UObject/Object.h"
#include "TitleStorageFileHeader.h"
#include "TitleStorageFile.generated.h"

class UDataTable;

UCLASS(Blueprintable)
class GUNFIREONLINEMODULE_API UTitleStorageFile : public UObject {
    GENERATED_BODY()
public:
    UPROPERTY(BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    FTitleStorageFileHeader Header;
    
    UTitleStorageFile();

    UFUNCTION(BlueprintCallable, BlueprintPure)
    void AsString(FString& String);
    
    UFUNCTION(BlueprintCallable, BlueprintPure)
    bool AsDataTable(UDataTable* DataTable);
    
};

