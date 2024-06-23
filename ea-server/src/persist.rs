use std::{
    collections::HashMap,
    marker::PhantomData,
    sync::{Arc, RwLock},
};

use async_trait::async_trait;
use cqrs_es::{
    persist::{PersistenceError, ViewContext, ViewRepository},
    Aggregate, View,
};

type LockedViewMap<V> = RwLock<HashMap<String, (V, i64)>>;

#[derive(Clone)]
pub struct InMemoryViewRepository<V, A>
where
    V: View<A>,
    A: Aggregate,
{
    views: Arc<LockedViewMap<V>>,
    _phantom: PhantomData<(V, A)>,
}

impl<V, A> InMemoryViewRepository<V, A>
where
    V: View<A>,
    A: Aggregate,
{
    pub fn default() -> InMemoryViewRepository<V, A> {
        let views = Arc::default();
        Self {
            views,
            _phantom: Default::default(),
        }
    }
}

#[async_trait]
impl<V, A> ViewRepository<V, A> for InMemoryViewRepository<V, A>
where
    V: View<A> + Clone,
    A: Aggregate,
{
    async fn load(&self, view_id: &str) -> Result<Option<V>, PersistenceError> {
        let views = self.views.read().unwrap();
        match views.get(view_id) {
            None => Ok(None),
            Some(view) => Ok(Some(view.0.clone())),
        }
    }

    async fn load_with_context(
        &self,
        view_id: &str,
    ) -> Result<Option<(V, ViewContext)>, PersistenceError> {
        let views = self.views.read().unwrap();
        match views.get(view_id) {
            None => Ok(None),
            Some(view) => {
                let view_context = ViewContext::new(view_id.to_string(), view.1);
                Ok(Some((view.0.clone(), view_context)))
            }
        }
    }

    async fn update_view(&self, view: V, context: ViewContext) -> Result<(), PersistenceError> {
        let mut views = self.views.write().unwrap();
        views.insert(context.view_instance_id, (view, context.version + 1));
        Ok(())
    }
}

#[cfg(test)]
mod test {
    use cqrs_es::persist::{ViewContext, ViewRepository};

    use crate::persist::InMemoryViewRepository;
    use crate::testing::{Created, TestAggregate, TestEvent, TestView};

    #[tokio::test]
    async fn test_valid_view_repository() {
        let repo = InMemoryViewRepository::<TestView, TestAggregate>::default();
        let test_view_id = uuid::Uuid::new_v4().to_string();

        let view = TestView::new(vec![TestEvent::Created(Created {
            id: "just a test event for this view".to_string(),
        })]);
        repo.update_view(view.clone(), ViewContext::new(test_view_id.to_string(), 0))
            .await
            .unwrap();
        let (found, context) = repo
            .load_with_context(&test_view_id)
            .await
            .unwrap()
            .unwrap();
        assert_eq!(found, view);
        let found = repo.load(&test_view_id).await.unwrap().unwrap();
        assert_eq!(found, view);

        let updated_view = TestView::new(vec![TestEvent::Created(Created {
            id: "a totally different view".to_string(),
        })]);
        repo.update_view(updated_view.clone(), context)
            .await
            .unwrap();
        let found_option = repo.load(&test_view_id).await.unwrap();
        let found = found_option.unwrap();

        assert_eq!(found, updated_view);
    }
}
