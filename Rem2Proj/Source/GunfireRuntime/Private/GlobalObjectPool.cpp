#include "GlobalObjectPool.h"

UGlobalObjectPool::UGlobalObjectPool(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
}

UGlobalObjectPool* UGlobalObjectPool::GetInstance(UWorld* World) {
    return NULL;
}

void UGlobalObjectPool::Empty() {
}


