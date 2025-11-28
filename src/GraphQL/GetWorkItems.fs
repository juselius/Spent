[<RequireQualifiedAccess>]
module rec GitLab.Data.GraphQLClient.GetWorkItems

type InputVariables = { group: string }

/// A list of nodes.
type WorkItem = {
    /// Global ID of the work item.
    id: string
    /// Internal ID of the work item.
    iid: string
    /// Title of the work item.
    title: string
    /// State of the work item.
    state: WorkItemState
    /// URL of the object.
    webUrl: Option<string>
    /// Timestamp of when the work item was created.
    createdAt: string
    /// Timestamp of when the work item was last updated.
    updatedAt: string
}

/// Work items that belong to the namespace. Introduced in GitLab 16.3: **Status**: Experiment.
type WorkItemConnection = {
    /// A list of nodes.
    nodes: Option<list<Option<WorkItem>>>
}

/// Find a group.
type Group = {
    /// Work items that belong to the namespace. Introduced in GitLab 16.3: **Status**: Experiment.
    workItems: Option<WorkItemConnection>
}

type Query = {
    /// Find a group.
    group: Option<Group>
}