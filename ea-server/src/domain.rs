use std::borrow::{Borrow, BorrowMut};
use std::collections::{HashMap, HashSet};

use eventually::aggregate;
use eventually::message::Message;
use eventually_macros::aggregate_root;
use rust_decimal::Decimal;

pub type AccountRepository<S> = aggregate::EventSourcedRepository<Account, S>;

pub type CopyTradeId = String;
pub type TradeId = String;
pub type TradeSymbol = String;

#[derive(Debug, Clone, PartialEq, Eq)]
pub enum TradeType {
    Buy,
    Sell,
}

#[derive(Debug, Clone, PartialEq, Eq)]
pub struct TradeOpenDetails {
    pub ticket_no: i16,
    pub symbol: TradeSymbol,
    pub trade_type: TradeType,
    pub price: Decimal,
    pub lots: Decimal,
}

#[derive(Debug, Clone, PartialEq, Eq)]
pub struct TradeCloseDetails {
    pub price: Decimal,
    pub lots: Decimal,
}

pub type AccountId = String;
pub type AccountKey = String;
pub type EaVersion = String;

#[derive(Debug, Clone, PartialEq, Eq)]
pub enum AccountRole {
    Publisher,
    Subscriber,
}

#[derive(Debug, Clone, PartialEq, Eq)]
pub enum AccountEvent {
    Created {
        id: AccountId,
        key: AccountKey,
    },
    Activated {
        id: AccountId,
    },
    Deactivated {
        id: AccountId,
    },
    PermissiveRolesChanged {
        id: AccountId,
        permissive_roles: Vec<AccountRole>,
    },
    BalanceChanged {
        id: AccountId,
        balance: Decimal,
    },
    TradeOpened {
        id: AccountId,
        trade_id: TradeId,
        details: TradeOpenDetails,
    },
    TradeClosed {
        id: AccountId,
        trade_id: TradeId,
        details: TradeCloseDetails,
    },
    TradeOpenReceived {
        id: AccountId,
        copy_trade_id: CopyTradeId,
    },
    TradeOpenFailed {
        id: AccountId,
        copy_trade_id: CopyTradeId,
        message: String,
    },
    TradeOpenCopied {
        id: AccountId,
        copy_trade_id: CopyTradeId,
        trade_id: TradeId,
        details: TradeOpenDetails,
    },
    TradeCloseReceived {
        id: AccountId,
        copy_trade_id: CopyTradeId,
    },
    TradeCloseFailed {
        id: AccountId,
        copy_trade_id: CopyTradeId,
        message: String,
    },
    TradeCloseCopied {
        id: AccountId,
        copy_trade_id: CopyTradeId,
        trade_id: TradeId,
        details: TradeCloseDetails,
    },
}

impl Message for AccountEvent {
    fn name(&self) -> &'static str {
        match self {
            AccountEvent::Created { .. } => "AccountEventCreated",
            AccountEvent::Activated { .. } => "AccountEventActivated",
            AccountEvent::Deactivated { .. } => "AccountEventDeactivated",
            AccountEvent::PermissiveRolesChanged { .. } => "AccountEventPermissiveRolesChanged",
            AccountEvent::BalanceChanged { .. } => "AccountEventBalanceChanged",
            AccountEvent::TradeOpened { .. } => "AccountEventTradeOpened",
            AccountEvent::TradeClosed { .. } => "AccountEventTradeClosed",
            AccountEvent::TradeOpenReceived { .. } => "AccountEventTradeOpenReceived",
            AccountEvent::TradeOpenFailed { .. } => "AccountEventTradeOpenFailed",
            AccountEvent::TradeOpenCopied { .. } => "AccountEventTradeOpenCopied",
            AccountEvent::TradeCloseReceived { .. } => "AccountEventTradeCloseReceived",
            AccountEvent::TradeCloseFailed { .. } => "AccountEventTradeCloseFailed",
            AccountEvent::TradeCloseCopied { .. } => "AccountEventTradeCloseCopied",
        }
    }
}

#[derive(Debug, Clone, PartialEq, Eq, thiserror::Error)]
pub enum AccountError {
    #[error("account is not yet created")]
    NotCreatedYet,
    #[error("account has already been created")]
    AlreadyCreated,
    #[error("empty id provided for the new bank account")]
    EmptyAccountId,
    #[error("empty account holder id provided for the new bank account")]
    EmptyAccountHolderId,
    #[error("a deposit was attempted with negative import")]
    NegativeDepositAttempted,
    #[error("no money to deposit has been specified")]
    NoMoneyDeposited,
    #[error("transfer could not be sent due to insufficient funds")]
    InsufficientFunds,
    #[error("transfer Trade was destined to a different recipient: {0}")]
    WrongTradeRecipient(AccountId),
    #[error("the account is closed")]
    Closed,
    #[error("bank account has already been closed")]
    AlreadyClosed,
}

#[derive(Debug, Clone)]
pub struct Position {
    pub trade_id: TradeId,
    pub copy_trade_id: Option<CopyTradeId>,
    pub ticket_no: i16,
    pub symbol: TradeSymbol,
    pub trade_type: TradeType,
    pub price_opened: Decimal,
    pub lots: Decimal,
}

#[derive(Debug, Clone)]
pub struct Account {
    id: AccountId,
    key: AccountKey,
    permissive_roles: Vec<AccountRole>,
    role: Option<AccountRole>,
    current_balance: Option<Decimal>,
    trade_applied: HashSet<CopyTradeId>,
    current_position: HashMap<TradeId, Position>,
    is_active: bool,
}

impl aggregate::Aggregate for Account {
    type Id = AccountId;
    type Event = AccountEvent;
    type Error = AccountError;

    fn type_name() -> &'static str {
        "Account"
    }

    fn aggregate_id(&self) -> &Self::Id {
        &self.id
    }

    fn apply(state: Option<Self>, event: Self::Event) -> Result<Self, Self::Error> {
        match state {
            None => match event {
                AccountEvent::Created { id, key, .. } => Ok(Account {
                    id,
                    key,
                    is_active: true,
                    current_balance: None,
                    current_position: HashMap::new(),
                    permissive_roles: Vec::new(),
                    role: None,
                    trade_applied: HashSet::new(),
                }),
                _ => Err(AccountError::NotCreatedYet),
            },
            Some(mut account) => match event {
                AccountEvent::Activated { .. } => {
                    account.is_active = true;
                    Ok(account)
                }
                AccountEvent::PermissiveRolesChanged {
                    permissive_roles, ..
                } => {
                    account.permissive_roles = permissive_roles;
                    Ok(account)
                }
                AccountEvent::Deactivated { .. } => {
                    account.is_active = false;
                    Ok(account)
                }
                AccountEvent::BalanceChanged { balance, .. } => {
                    account.current_balance = Some(balance);
                    Ok(account)
                }
                AccountEvent::TradeOpened {
                    trade_id, details, ..
                } => {
                    account.current_position.insert(
                        trade_id.clone(),
                        Position {
                            trade_id,
                            copy_trade_id: None,
                            ticket_no: details.ticket_no,
                            symbol: details.symbol,
                            trade_type: details.trade_type,
                            price_opened: details.price,
                            lots: details.lots,
                        },
                    );
                    Ok(account)
                }
                AccountEvent::TradeClosed {
                    trade_id, details, ..
                } => {
                    if let Some(position) = account.current_position.get_mut(&trade_id) {
                        position.lots -= details.lots;
                        if position.lots <= 0.into() {
                            account.current_position.remove(&trade_id);
                        }
                    }
                    Ok(account)
                }
                AccountEvent::TradeOpenReceived { .. } => Ok(account),
                AccountEvent::TradeOpenFailed { .. } => Ok(account),
                AccountEvent::TradeOpenCopied {
                    copy_trade_id,
                    trade_id,
                    details,
                    ..
                } => {
                    account.current_position.insert(
                        trade_id.clone(),
                        Position {
                            trade_id: trade_id,
                            copy_trade_id: Option::Some(copy_trade_id),
                            ticket_no: details.ticket_no,
                            symbol: details.symbol,
                            trade_type: details.trade_type,
                            price_opened: details.price,
                            lots: details.lots,
                        },
                    );
                    Ok(account)
                }
                AccountEvent::TradeCloseReceived { .. } => Ok(account),
                AccountEvent::TradeCloseFailed { .. } => Ok(account),
                AccountEvent::TradeCloseCopied {
                    copy_trade_id,
                    trade_id,
                    details,
                    ..
                } => {
                    if let Some(position) = account.current_position.get_mut(&trade_id) {
                        position.lots -= details.lots;
                        if position.lots <= 0.into() {
                            account.current_position.remove(&trade_id);
                        }
                    }
                    Ok(account)
                }
                _ => Err(AccountError::AlreadyCreated),
            },
        }
    }
}

#[aggregate_root(Account)]
#[derive(Debug, Clone)]
pub struct AccountRoot;

impl AccountRoot {
    pub fn new(id: AccountId, key: AccountKey) -> Result<Self, AccountError> {
        if id.is_empty() {
            return Err(AccountError::EmptyAccountId);
        }

        aggregate::Root::<Account>::record_new(AccountEvent::Created { id, key }.into()).map(Self)
    }
}
