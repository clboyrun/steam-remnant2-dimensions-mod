#include "CraftingRecipeManager.h"

UCraftingRecipeManager::UCraftingRecipeManager(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
}

UCraftingRecipeManager* UCraftingRecipeManager::GetInstance(UObject* WorldContextObject) {
    return NULL;
}

TSet<TSoftClassPtr<AItem>> UCraftingRecipeManager::GetAllMaterials(UObject* WorldContextObject) {
    return TSet<TSoftClassPtr<AItem>>();
}


